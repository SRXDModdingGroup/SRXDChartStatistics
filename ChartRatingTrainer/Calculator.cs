using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ChartAutoRating;

namespace ChartRatingTrainer {
    public class Calculator {
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

        public ReadOnlyCollection<CurveWeights> MetricCurveWeights { get; }

        private int threadIndex;
        private Network network;
        private CurveWeights[] curveWeights;

        public Calculator(int threadIndex) {
            this.threadIndex = threadIndex;
            network = new Network(Program.METRIC_COUNT);
            curveWeights = new CurveWeights[Program.METRIC_COUNT];
            MetricCurveWeights = new ReadOnlyCollection<CurveWeights>(curveWeights);
        }
        
        public void Randomize(Random random, double magnitude) {
            for (int i = 0; i < Program.METRIC_COUNT; i++)
                curveWeights[i] = CurveWeights.Random(random, magnitude);
            
            NormalizeCurveWeights();
            ApplyWeights();
        }

        public void SetWeights(CurveWeights[] weights) {
            for (int i = 0; i < Program.METRIC_COUNT; i++)
                curveWeights[i] = weights[i];
            
            NormalizeCurveWeights();
            ApplyWeights();
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

            for (int i = 0; i < Program.METRIC_COUNT; i++) {
                double magnitude = curveWeights[i].Magnitude;

                if (magnitude > OVERWEIGHT_THRESHOLD)
                    overWeight += magnitude - OVERWEIGHT_THRESHOLD;
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
            for (int i = 0; i < Program.METRIC_COUNT; i++)
                network.SetCoefficients(i, curveWeights[i].ToCoefficients());
        }

        private void NormalizeCurveWeights() {
            double sum = 0d;

            foreach (var weights in curveWeights)
                sum += weights.Magnitude;

            for (int i = 0; i < curveWeights.Length; i++)
                curveWeights[i] /= sum;
        }

        private void CacheResults(DataSet dataSet) {
            double[] resultsArray = dataSet.ResultsArrays[threadIndex];
            
            for (int i = 0; i < dataSet.Size; i++)
                resultsArray[i] = CalculateValue(dataSet.Datas[i]);
        }

        public static void Cross(Random random, Calculator parent1, Calculator parent2, Calculator child) {
            for (int i = 0; i < Program.METRIC_COUNT; i++) {
                bool mutateMagnitude = random.NextDouble() < MUTATION_CHANCE;
                bool mutateCurve = random.NextDouble() < MUTATION_CHANCE;
                double interp = random.NextDouble();
                var newWeights = (1d - interp) * parent1.curveWeights[i] + interp * parent2.curveWeights[i];
                CurveWeights mutatedWeights;

                if (mutateMagnitude && mutateCurve)
                    mutatedWeights = CurveWeights.Random(random, random.NextDouble());
                else if (mutateMagnitude)
                    mutatedWeights = random.NextDouble() * CurveWeights.Normalize(newWeights);
                else if (mutateCurve)
                    mutatedWeights = CurveWeights.Random(random, newWeights.Magnitude);
                else {
                    child.curveWeights[i] = newWeights;
                    
                    continue;
                }

                interp = MAX_MUTATION_AMOUNT * random.NextDouble();
                child.curveWeights[i] = (1d - interp) * newWeights + interp * mutatedWeights;
            }
            
            child.NormalizeCurveWeights();
            child.ApplyWeights();
        }
    }
}