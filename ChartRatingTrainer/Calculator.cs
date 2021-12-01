using System;
using System.IO;
using System.Threading.Tasks;
using ChartAutoRating;
using ChartMetrics;

namespace ChartRatingTrainer {
    public class Calculator {
        public static readonly int METRIC_COUNT = ChartProcessor.DifficultyMetrics.Count;

        private static readonly double MUTATION_FACTOR = 1d / 16d;
        private static readonly double MUTATION_CHANCE = 1d / (METRIC_COUNT * (METRIC_COUNT + 1) / 2 + METRIC_COUNT);
        private static readonly double MUTATION_AMOUNT = MUTATION_FACTOR / METRIC_COUNT;

        public Curve[,] ValueCurves { get; }
        
        public Curve[] WeightCurves { get; }

        private Network network;

        private Calculator() {
            network = Network.Create(METRIC_COUNT);
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

        public void SerializeNetwork(BinaryWriter writer) => network.Serialize(writer);
        
        public void CacheResults(DataSet dataSet) {
            var valuePairs = dataSet.ExpectedReturnedPairs;

            dataSet.InitValuePairs();
            Parallel.For(0, dataSet.Size, i => valuePairs[i].ReturnedValue = network.GetValue(dataSet.Datas[i]));
            Array.Sort(valuePairs);
            
            double scale = 1d / (dataSet.Size - 1);

            for (int i = 1; i < dataSet.Size - 1; i++) {
                double first = valuePairs[i - 1].ReturnedValue;
                var mid = valuePairs[i];

                mid.ReturnedPosition = scale * (i - 0.5d + (mid.ReturnedValue - first) / (valuePairs[i + 1].ReturnedValue - first));
            }

            valuePairs[0].ReturnedPosition = 0d;
            valuePairs[dataSet.Size - 1].ReturnedPosition = 1d;
        }

        public double CalculateFitness(DataSet[] dataSets) {
            double sum = 0d;
            int count = 0;

            foreach (var dataSet in dataSets) {
                CacheResults(dataSet);

                var valuePairs = dataSet.ExpectedReturnedPairs;

                for (int i = 0; i < dataSet.Size; i++) {
                    var pair = valuePairs[i];
                    double error = pair.ReturnedPosition - pair.Expected;
                    
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
                for (int j = i; j < METRIC_COUNT; j++) {
                    childValueCurves[i, j] = 0.5d * (parentValueCurves1[i, j] + parentValueCurves2[i, j]);

                    if (random.NextDouble() < MUTATION_CHANCE)
                        parentValueCurves2[i, j] = Curve.Clamp(parentValueCurves2[i, j] + Curve.Random(random, MUTATION_AMOUNT * (2d * random.NextDouble() - 1d)));
                }

                childWeightCurves[i] = 0.5d * (parentWeightCurves1[i] + parentWeightCurves2[i]);

                if (random.NextDouble() < MUTATION_CHANCE)
                    parentWeightCurves2[i] = Curve.Clamp(parentWeightCurves2[i] + Curve.Random(random, MUTATION_AMOUNT * (2d * random.NextDouble() - 1d)));
            }

            parent2.ApplyCurves();
            child.ApplyCurves();
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
                    network.SetValueCoefficients(i, j, ValueCurves[i, j].ToCoefficients());
                }
            }

            totalMagnitude = 0d;
            
            for (int i = 0; i < METRIC_COUNT; i++)
                totalMagnitude += WeightCurves[i].Magnitude;

            scale = 1d / totalMagnitude;

            for (int i = 0; i < METRIC_COUNT; i++) {
                WeightCurves[i] = scale * WeightCurves[i];
                network.SetWeightCoefficients(i, WeightCurves[i].ToCoefficients());
            }
        }
    }
}