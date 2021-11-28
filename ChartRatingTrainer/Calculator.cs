using System;
using System.Collections.Generic;
using System.IO;
using ChartAutoRating;
using ChartMetrics;

namespace ChartRatingTrainer {
    public class Calculator {
        public static readonly int METRIC_COUNT = ChartProcessor.Metrics.Count;
        
        private static readonly double MUTATION_CHANCE = 0.03125d;
        private static readonly double MAX_MUTATION_AMOUNT = 0.0625d;
        private static readonly double OVERWEIGHT_THRESHOLD = 0.35d;
        private static readonly double OVERWEIGHT_BIAS = 0.08d;
        private static readonly double WINDOW_MIDPOINT = 0.0009765625d;

        public Curve[,] ValueCurves { get; }
        
        public Curve[] WeightCurves { get; }

        private Network network;

        private Calculator() {
            network = Network.Create(METRIC_COUNT);
            ValueCurves = new Curve[METRIC_COUNT, METRIC_COUNT];
            WeightCurves = new Curve[METRIC_COUNT];
        }

        public static Calculator Random(int threadIndex, Random random) {
            var calculator = new Calculator();
            
            for (int i = 0; i < METRIC_COUNT; i++) {
                for (int j = 1; j < METRIC_COUNT; j++)
                    calculator.ValueCurves[i, j] = Curve.Random(random, random.NextDouble());

                calculator.WeightCurves[i] = Curve.Random(random, random.NextDouble());
            }

            calculator.ApplyCurves();

            return calculator;
        }

        public static Calculator Deserialize(int threadIndex, BinaryReader reader) {
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

        public double CalculateFitness(DataSet[] dataSets, int threadIndex) {
            double totalSum = 0d;
            double totalAbsSum = 0;
            
            foreach (var dataSet in dataSets) {
                var resultsTable = dataSet.ResultsTables[threadIndex];

                CacheResults(dataSet, threadIndex);
                Table.GenerateWindowedComparisonTable(resultsTable, dataSet.ResultsArrays[threadIndex], WINDOW_MIDPOINT, dataSet.Size);
                Table.CorrelationComponents(resultsTable, dataSet.DifficultyComparisons, dataSet.Size, out double sum1, out double absSum);
                totalSum += sum1;
                totalAbsSum += absSum;
            }

            double correlation = totalSum / totalAbsSum;
            double overWeight = 0d;

            for (int i = 0; i < METRIC_COUNT; i++) {
                double sum = 0d;
                
                for (int j = 0; j < METRIC_COUNT; j++) {
                    if (j >= i)
                        sum += ValueCurves[i, j].Magnitude;
                    else
                        sum += ValueCurves[j, i].Magnitude;
                }
                
                if (sum > OVERWEIGHT_THRESHOLD)
                    overWeight += sum - OVERWEIGHT_THRESHOLD;
            }

            return 0.5d * (correlation + 1d) - OVERWEIGHT_BIAS * overWeight;
        }

        public double CalculateCorrelation(DataSet[] dataSets, int threadIndex) {
            double totalSum = 0d;
            double totalAbsSum = 0;
            
            foreach (var dataSet in dataSets) {
                var resultsTable = dataSet.ResultsTables[threadIndex];

                CacheResults(dataSet, threadIndex);
                Table.GenerateComparisonTable(resultsTable, dataSet.ResultsArrays[threadIndex], dataSet.Size);
                Table.CorrelationComponents(resultsTable, dataSet.DifficultyComparisons, dataSet.Size, out double sum, out double absSum);
                totalSum += sum;
                totalAbsSum += absSum;
            }
            
            return totalSum / totalAbsSum;
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
                    bool mutateMagnitude = random.NextDouble() < MUTATION_CHANCE;
                    bool mutateCurve = random.NextDouble() < MUTATION_CHANCE;
                    double interp = random.NextDouble();
                    
                    var newCurves = (1d - interp) * parentCurve1 + interp * parentCurve2;
                    Curve mutated;

                    if (mutateMagnitude && mutateCurve)
                        mutated = Curve.Random(random, random.NextDouble());
                    else if (mutateMagnitude)
                        mutated = random.NextDouble() * Curve.Normalize(newCurves);
                    else if (mutateCurve)
                        mutated = Curve.Random(random, newCurves.Magnitude);
                    else {
                        childCurve = newCurves;
                    
                        return;
                    }

                    interp = MAX_MUTATION_AMOUNT * random.NextDouble();
                    childCurve = (1d - interp) * newCurves + interp * mutated;
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

        private void CacheResults(DataSet dataSet, int threadIndex) {
            double[] resultsArray = dataSet.ResultsArrays[threadIndex];
            
            for (int i = 0; i < dataSet.Size; i++)
                resultsArray[i] = CalculateValue(dataSet.Datas[i]);
        }

        private double CalculateValue(Data data) => network.GetValue(data);
    }
}