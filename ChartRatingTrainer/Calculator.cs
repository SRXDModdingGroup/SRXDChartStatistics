using System;
using System.IO;
using System.Threading.Tasks;
using ChartAutoRating;
using ChartMetrics;

namespace ChartRatingTrainer {
    public class Calculator {
        public static readonly int METRIC_COUNT = ChartProcessor.Metrics.Count;
        
        private static readonly double MUTATION_CHANCE = 0.0625d;
        private static readonly double MUTATION_AMOUNT = 0.25d;
        private static readonly double OVERWEIGHT_THRESHOLD = 0.35d;
        private static readonly double OVERWEIGHT_BIAS = 0.5d;
        private static readonly double WINDOW_MIDPOINT = 0.00390625d;

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
                for (int j = 1; j < METRIC_COUNT; j++)
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

                    calculator.ValueCurves[i, j] = new Curve(w0, w1, w2);
                }
            }

            for (int i = 0; i < METRIC_COUNT; i++) {
                double w0 = reader.ReadDouble();
                double w1 = reader.ReadDouble();
                double w2 = reader.ReadDouble();

                calculator.WeightCurves[i] = new Curve(w0, w1, w2);
            }
            
            calculator.ApplyCurves();

            return calculator;
        }

        public void Serialize(BinaryWriter writer) {
            for (int i = 0; i < METRIC_COUNT; i++) {
                for (int j = i; j < METRIC_COUNT; j++) {
                    var curve = ValueCurves[i, j];
                    
                    writer.Write(curve.W0);
                    writer.Write(curve.W1);
                    writer.Write(curve.W2);
                }
            }
            
            for (int i = 0; i < METRIC_COUNT; i++) {
                var curve = WeightCurves[i];
                    
                writer.Write(curve.W0);
                writer.Write(curve.W1);
                writer.Write(curve.W2);
            }
        }

        public void SerializeNetwork(BinaryWriter writer) => network.Serialize(writer);
        
        public void CacheResults(DataSet dataSet) {
            //var resultsTable = dataSet.ResultsTable;
            double[] resultsArray = dataSet.ResultsArray1;

            Parallel.For(0, dataSet.Size, i => resultsArray[i] = network.GetValue(dataSet.Datas[i]));
            //Table.GenerateComparisonTable(resultsTable, resultsArray, WINDOW_MIDPOINT, dataSet.Size);
            GetPositionArray(dataSet.ResultsArray2, resultsArray, dataSet.Size);
        }

        public double CalculateFitness(DataSet[] dataSets) {
            double min = double.PositiveInfinity;
            double sum = 0d;
            int count = 0;

            foreach (var dataSet in dataSets) {
                double[] resultsArray = dataSet.ResultsArray2;
                double[] positionValues = dataSet.PositionValues;
                    
                CacheResults(dataSet);

                for (int i = 0; i < dataSet.Size; i++) {
                    double value = resultsArray[i] - positionValues[i];

                    value = 1d / (value * value + 1d);

                    if (value < min)
                        min = value;

                    sum += value;
                }

                count += dataSet.Size;
            }

            double overWeight = 0d;

            for (int i = 0; i < METRIC_COUNT; i++) {
                for (int j = 0; j < METRIC_COUNT; j++) {
                    double value;
                    
                    if (j >= i)
                        value = ValueCurves[i, j].Magnitude;
                    else
                        value = ValueCurves[j, i].Magnitude;

                    if (value > OVERWEIGHT_THRESHOLD)
                        overWeight += value - OVERWEIGHT_THRESHOLD;
                }
            }

            return sum * min / count - OVERWEIGHT_BIAS * overWeight;
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
                    CrossCurves(parentValueCurves1[i, j], parentValueCurves2[i, j], out childValueCurves[i, j]);
                
                CrossCurves(parentWeightCurves1[i], parentWeightCurves2[i], out childWeightCurves[i]);

                void CrossCurves(Curve parentCurve1, Curve parentCurve2, out Curve childCurve) {
                    if (random.NextDouble() < 0.5d)
                        childCurve = parentCurve1;
                    else
                        childCurve = parentCurve2;
                    
                    if (random.NextDouble() < MUTATION_CHANCE)
                        childCurve = Curve.Clamp(childCurve + Curve.Random(random, MUTATION_AMOUNT * (2d * random.NextDouble() - 1d)));
                }
            }

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
        
        private static void GetPositionArray(double[] target, double[] input, int size) {
            for (int i = 0; i < size; i++) {
                double value = input[i];
                int sum = 0;
                
                for (int j = 0; j < size; j++) {
                    double other = input[j];

                    if (value > other)
                        sum++;
                }

                target[i] = (double) sum / size;
            }
        }
    }
}