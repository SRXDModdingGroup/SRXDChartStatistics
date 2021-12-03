using System;
using System.IO;
using System.Threading.Tasks;
using ChartAutoRating;
using ChartMetrics;

namespace ChartRatingTrainer {
    public class Calculator {
        public static readonly int METRIC_COUNT = ChartProcessor.DifficultyMetrics.Count;

        private static readonly double MUTATION_CHANCE = 1d / (METRIC_COUNT + METRIC_COUNT * (METRIC_COUNT + 1) / 2);
        private static readonly double MUTATION_AMOUNT = 0.00390625d;

        public Curve[,] ValueCurves { get; }
        
        public Curve[] WeightCurves { get; }
        private Matrix matrix;

        private Calculator() {
            matrix = new Matrix(METRIC_COUNT);
            ValueCurves = new Curve[METRIC_COUNT, METRIC_COUNT];
            WeightCurves = new Curve[METRIC_COUNT];
        }

        public static Calculator Random(Random random) {
            var calculator = new Calculator();
            
            for (int i = 0; i < METRIC_COUNT; i++) {
                for (int j = i; j < METRIC_COUNT; j++)
                    calculator.ValueCurves[i, j] = Curve.Random(random, random.NextDouble());

                calculator.WeightCurves[i] = Curve.Random(random, random.NextDouble());
            }

            calculator.ApplyCurves();

            return calculator;
        }

        public static Calculator Deserialize(BinaryReader reader) {
            var calculator = new Calculator();

            for (int i = 0; i < METRIC_COUNT; i++) {
                for (int j = i; j < METRIC_COUNT; j++) {
                    double w0 = reader.ReadDouble();
                    double w1 = reader.ReadDouble();
                    double w2 = reader.ReadDouble();
                    double w3 = reader.ReadDouble();
                    double w4 = reader.ReadDouble();
                    double w5 = reader.ReadDouble();

                    calculator.ValueCurves[i, j] = new Curve(w0, w1, w2, w3, w4, w5);
                }
            }

            for (int i = 0; i < METRIC_COUNT; i++) {
                double w0 = reader.ReadDouble();
                double w1 = reader.ReadDouble();
                double w2 = reader.ReadDouble();
                double w3 = reader.ReadDouble();
                double w4 = reader.ReadDouble();
                double w5 = reader.ReadDouble();

                calculator.WeightCurves[i] = new Curve(w0, w1, w2, w3, w4, w5);
            }
            
            calculator.ApplyCurves();

            return calculator;
        }

        public void Serialize(BinaryWriter writer) {
            for (int i = 0; i < METRIC_COUNT; i++) {
                for (int j = i; j < METRIC_COUNT; j++) {
                    var curve = ValueCurves[i, j];
                    
                    writer.Write(curve.A);
                    writer.Write(curve.B);
                    writer.Write(curve.C);
                    writer.Write(curve.D);
                    writer.Write(curve.E);
                    writer.Write(curve.F);
                }
            }
            
            for (int i = 0; i < METRIC_COUNT; i++) {
                var curve = WeightCurves[i];
                
                writer.Write(curve.A);
                writer.Write(curve.B);
                writer.Write(curve.C);
                writer.Write(curve.D);
                writer.Write(curve.E);
                writer.Write(curve.F);
            }
        }

        public void SerializeNetwork(BinaryWriter writer) => matrix.Serialize(writer);
        
        public void CacheResults(DataSet dataSet) {
            var expectedReturned = dataSet.ExpectedReturned;

            dataSet.InitValuePairs();
            Parallel.For(0, dataSet.Size, i => expectedReturned[i].Returned = dataSet.Datas[i].GetResult(matrix));

            int count = expectedReturned.Length;
            double sx = 0d;
            double sy = 0d;
            double sxx = 0d;
            double sxy = 0d;
            
            foreach (var pair in expectedReturned) {
                double x = pair.Expected;
                double y = pair.Returned;

                sx += x;
                sy += y;
                sxx += x * x;
                sxy += x * y;
            }

            dataSet.Scale = (count * sxy - sx * sy) / (count * sxx - sx * sx);
            dataSet.Bias = (sxy * sx - sy * sxx) / (sx * sx - count * sxx);

            foreach (var pair in expectedReturned)
                pair.Returned = (pair.Returned - dataSet.Bias) / dataSet.Scale;
        }

        public double CalculateFitness(DataSet[] dataSets) {
            double sum = 0d;
            int count = 0;

            foreach (var dataSet in dataSets) {
                CacheResults(dataSet);
                
                var valuePairs = dataSet.ExpectedReturned;
                
                for (int i = 0; i < dataSet.Size; i++) {
                    var pair = valuePairs[i];
                    double error = pair.Returned - pair.Expected;
                    
                    sum += error * error;
                }
                
                count += dataSet.Size;
            }

            return 1d - Math.Sqrt(sum / count);
        }

        public static void Cross(Calculator parent1, Calculator parent2, Calculator child, Random random) {
            var parentValueCurves1 = parent1.ValueCurves;
            var parentValueCurves2 = parent2.ValueCurves;
            var childValueCurves = child.ValueCurves;
            var parentWeightCurves1 = parent1.WeightCurves;
            var parentWeightCurves2 = parent2.WeightCurves;
            var childWeightCurves = child.WeightCurves;

            for (int i = 0; i < METRIC_COUNT; i++) {
                for (int j = i; j < METRIC_COUNT; j++)
                    CrossCurves(parentValueCurves1[i, j], ref parentValueCurves2[i, j], out childValueCurves[i, j]);

                CrossCurves(parentWeightCurves1[i], ref parentWeightCurves2[i], out childWeightCurves[i]);
            }

            parent2.ApplyCurves();
            child.ApplyCurves();

            void CrossCurves(Curve parentCurve1, ref Curve parentCurve2, out Curve childCurve) {
                double interp = random.NextDouble();
                
                childCurve = (1d - interp) * parentCurve1 + interp * parentCurve2;

                if (random.NextDouble() > MUTATION_CHANCE)
                    return;
                    
                interp = random.NextDouble();
                parentCurve2 = Curve.Clamp(parentCurve2 + Curve.Random(random, MUTATION_AMOUNT * (2d * interp * interp - 1d)));
            }
        }

        private void ApplyCurves() {
            double totalMagnitude = 0d;
            
            for (int i = 0; i < METRIC_COUNT; i++) {
                for (int j = i; j < METRIC_COUNT; j++)
                    totalMagnitude += ValueCurves[i, j].Magnitude;
            }

            double scale = 1d / totalMagnitude;
            
            for (int i = 0; i < METRIC_COUNT; i++) {
                for (int j = i; j < METRIC_COUNT; j++) {
                    ValueCurves[i, j] = scale * ValueCurves[i, j];
                    matrix.ValueCoefficients[i, j] = ValueCurves[i, j].ToCoefficients();
                }
            }

            totalMagnitude = 0d;
            
            for (int i = 0; i < METRIC_COUNT; i++)
                totalMagnitude += WeightCurves[i].Magnitude;

            scale = 1d / totalMagnitude;

            for (int i = 0; i < METRIC_COUNT; i++) {
                WeightCurves[i] = scale * WeightCurves[i];
                matrix.WeightCoefficients[i] = WeightCurves[i].ToCoefficients();
            }
        }
    }
}