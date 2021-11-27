using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.CompilerServices;
using ChartAutoRating;
using ChartMetrics;

namespace ChartRatingTrainer {
    public class Calculator {
        public static readonly int METRIC_COUNT = ChartProcessor.Metrics.Count;
        
        public readonly struct Anchor : IComparable<Anchor> {
            public string ChartTitle { get; }
            
            public double From { get; }
            
            public int To { get; }
            
            public double Correlation { get; }

            public Anchor(string chartTitle, double @from, int to, double correlation) {
                From = from;
                To = to;
                Correlation = correlation;
                ChartTitle = chartTitle;
            }

            public int CompareTo(Anchor other) => -Correlation.CompareTo(other.Correlation);
        }
        
        private static readonly double MUTATION_CHANCE = 0.25d;
        private static readonly double MAX_MUTATION_AMOUNT = 0.125d;
        private static readonly double OVERWEIGHT_THRESHOLD = 0.35d;
        private static readonly double OVERWEIGHT_BIAS = 0.0625d;
        
        public CurveWeights[,] CurveWeights { get; private set; }

        private int threadIndex;
        private Network network;

        public static Calculator Create(int threadIndex) =>
            new Calculator {
                threadIndex = threadIndex,
                network = new Network(METRIC_COUNT),
                CurveWeights = new CurveWeights[METRIC_COUNT, METRIC_COUNT]
            };

        public static Calculator Deserialize(int threadIndex, BinaryReader reader) {
            var calculator = Create(threadIndex);
            
            for (int i = 0; i < METRIC_COUNT; i++) {
                for (int j = i; j < METRIC_COUNT; j++) {
                    double w0 = reader.ReadDouble();
                    double w1 = reader.ReadDouble();
                    double w2 = reader.ReadDouble();

                    calculator.CurveWeights[i, j] = new CurveWeights(w0, w1, w2);
                }
            }

            return calculator;
        }

        public void Randomize(Random random, double magnitude) {
            for (int i = 0; i < METRIC_COUNT; i++) {
                for (int j = 0; j < METRIC_COUNT; j++)
                    CurveWeights[i, j] = ChartRatingTrainer.CurveWeights.Random(random, magnitude);
            }

            ApplyWeights();
        }

        public void SetWeights(CurveWeights[,] weights) {
            for (int i = 0; i < METRIC_COUNT; i++) {
                for (int j = 0; j < METRIC_COUNT; j++)
                    CurveWeights[i, j] = weights[i, j];
            }

            ApplyWeights();
        }

        public void SerializeCurveWeights(BinaryWriter writer) {
            for (int i = 0; i < METRIC_COUNT; i++) {
                for (int j = i; j < METRIC_COUNT; j++) {
                    var curveWeights = CurveWeights[i, j];
                    
                    writer.Write(curveWeights.W0);
                    writer.Write(curveWeights.W1);
                    writer.Write(curveWeights.W2);
                }
            }
        }
        
        public void SerializeCoefficients(BinaryWriter writer) {
            for (int i = 0; i < METRIC_COUNT; i++) {
                for (int j = i; j < METRIC_COUNT; j++) {
                    var coefficients = CurveWeights[i, j].ToCoefficients();
                    
                    writer.Write(coefficients.X1);
                    writer.Write(coefficients.X2);
                    writer.Write(coefficients.X3);
                }
            }
        }

        public double CalculateValue(Data data) => network.GetValue(data);

        public double CalculateCorrelation(DataSet[] dataSets) {
            double totalSum = 0d;
            double totalAbsSum = 0d;
            
            foreach (var dataSet in dataSets) {
                var resultsTable = dataSet.ResultsTables[threadIndex];

                CacheResults(dataSet);
                Table.GenerateComparisonTable(resultsTable, dataSet.ResultsArrays[threadIndex], dataSet.Size);
                Table.CorrelationComponents(resultsTable, dataSet.DifficultyComparisons, dataSet.Size, out double sum, out double absSum);
                totalSum += sum;
                totalAbsSum += absSum;
            }
            
            return totalSum / totalAbsSum;
        }

        public double CalculateFitness(DataSet[] dataSets) {
            double correlation = CalculateCorrelation(dataSets);
            double overWeight = 0d;

            for (int i = 0; i < METRIC_COUNT; i++) {
                for (int j = i; j < METRIC_COUNT; j++) {
                    double magnitude = CurveWeights[i, j].Magnitude;

                    if (magnitude > OVERWEIGHT_THRESHOLD)
                        overWeight += magnitude - OVERWEIGHT_THRESHOLD;
                }
            }

            return 0.5d * (correlation + 1d) - OVERWEIGHT_BIAS * overWeight;
        }

        public List<Anchor> CalculateAnchors(DataSet[] dataSets) {
            var anchors = new List<Anchor>();
            
            foreach (var dataSet in dataSets) {
                var resultsTable = dataSet.ResultsTables[threadIndex];

                CacheResults(dataSet);
                Table.GenerateComparisonTable(resultsTable, dataSet.ResultsArrays[threadIndex], dataSet.Size);

                for (int i = 0; i < dataSet.Size; i++) {
                    double correlation = Table.CorrelationForRow(resultsTable, dataSet.DifficultyComparisons, i, dataSet.Size);

                    anchors.Add(new Anchor(dataSet.RelevantChartInfo[i].Title, CalculateValue(dataSet.Datas[i]), dataSet.RelevantChartInfo[i].DifficultyRating, correlation));
                }
            }
            
            anchors.Sort();

            return anchors;
        }

        private void ApplyWeights() {
            for (int i = 0; i < METRIC_COUNT; i++) {
                for (int j = i; j < METRIC_COUNT; j++)
                    network.SetValueCoefficients(i, j, CurveWeights[i, j].ToCoefficients());
            }
        }

        private void CacheResults(DataSet dataSet) {
            double[] resultsArray = dataSet.ResultsArrays[threadIndex];
            
            for (int i = 0; i < dataSet.Size; i++)
                resultsArray[i] = CalculateValue(dataSet.Datas[i]);
        }

        public static void Cross(Random random, Calculator parent1, Calculator parent2, Calculator child) {
            for (int i = 0; i < METRIC_COUNT; i++) {
                for (int j = i; j < METRIC_COUNT; j++) {
                    bool mutateMagnitude = random.NextDouble() < MUTATION_CHANCE;
                    bool mutateCurve = random.NextDouble() < MUTATION_CHANCE;
                    double interp = random.NextDouble();
                    var newWeights = (1d - interp) * parent1.CurveWeights[i, j] + interp * parent2.CurveWeights[i, j];
                    CurveWeights mutatedWeights;

                    if (mutateMagnitude && mutateCurve)
                        mutatedWeights = ChartRatingTrainer.CurveWeights.Random(random, random.NextDouble());
                    else if (mutateMagnitude)
                        mutatedWeights = random.NextDouble() * ChartRatingTrainer.CurveWeights.Normalize(newWeights);
                    else if (mutateCurve)
                        mutatedWeights = ChartRatingTrainer.CurveWeights.Random(random, newWeights.Magnitude);
                    else {
                        child.CurveWeights[i, j] = newWeights;
                    
                        continue;
                    }

                    interp = MAX_MUTATION_AMOUNT * random.NextDouble();
                    child.CurveWeights[i, j] = (1d - interp) * newWeights + interp * mutatedWeights;
                }
            }

            child.ApplyWeights();
        }
    }
}