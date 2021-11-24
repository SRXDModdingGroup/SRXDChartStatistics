using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ChartAutoRating {
    public class Calculator {
        private static readonly Dictionary<int, Data> DATA_POOL = new Dictionary<int, Data>();

        private class Data {
            public Coefficients[] NormalizedCoefficients { get; }
            public double[] Results { get; }
            public double[] Magnitudes { get; }
            public double[] NewMagnitudes { get; }
            public double[] MagnitudeVectors { get; }
            public Table ResultsTable { get; }
            public Table VectorTable { get; }

            public Data(int size) {
                NormalizedCoefficients = new Coefficients[Program.METRIC_COUNT];
                Results = new double[size];
                Magnitudes = new double[Program.METRIC_COUNT];
                NewMagnitudes = new double[Program.METRIC_COUNT];
                MagnitudeVectors = new double[Program.METRIC_COUNT];
                ResultsTable = new Table(size);
                VectorTable = new Table(size);
            }
        }
        
        private static readonly int MAXIMIZE_ITERATIONS = 8;
        private static readonly int MAX_NUDGE_DIVISIONS = 8;
        private static readonly double MUTATION_CHANCE = 0.0625d;

        public ReadOnlyCollection<CurveWeights> MetricCurveWeights => new ReadOnlyCollection<CurveWeights>(metricCurveWeights);

        private CurveWeights[] metricCurveWeights;
        private Coefficients[] metricCoefficients;
        private Data data;

        public Calculator(int size, int threadIndex) {
            metricCurveWeights = new CurveWeights[Program.METRIC_COUNT];
            metricCoefficients = new Coefficients[Program.METRIC_COUNT];

            if (DATA_POOL.TryGetValue(size + threadIndex << 16, out data))
                return;
            
            data = new Data(size);
            DATA_POOL.Add(size + threadIndex << 16, data);
        }
        
        public void Randomize(Random random, double magnitude) {
            for (int i = 0; i < Program.METRIC_COUNT; i++)
                metricCurveWeights[i] = CurveWeights.Random(random, magnitude);
            
            NormalizeCurveWeights();
            ApplyWeights();
        }

        public void SetWeights(CurveWeights[] weights) {
            for (int i = 0; i < Program.METRIC_COUNT; i++)
                metricCurveWeights[i] = weights[i];
            
            NormalizeCurveWeights();
            ApplyWeights();
        }

        public double Maximize(DataSet dataSet) {
            double[] magnitudes = data.Magnitudes;
            double[] newMagnitudes = data.NewMagnitudes;
            
            for (int i = 0; i < Program.METRIC_COUNT; i++) {
                double magnitude = metricCoefficients[i].Magnitude;
                
                magnitudes[i] = metricCoefficients[i].Magnitude;
                data.NormalizedCoefficients[i] = metricCoefficients[i] / magnitude;
            }

            double correlation = CalculateCorrelation(dataSet);
            
            CacheResults(dataSet);
            Table.GenerateComparisonTable(data.ResultsTable, data.Results, dataSet.Size);

            for (int i = 0; i < MAXIMIZE_ITERATIONS; i++) {
                Table.Compare(data.VectorTable, dataSet.DifficultyComparisons, data.ResultsTable, dataSet.Size);
                
                for (int j = 0; j < Program.METRIC_COUNT; j++)
                    data.MagnitudeVectors[j] = Table.Correlation(dataSet.MetricComparisons[j], data.VectorTable, dataSet.Size);

                double newCorrelation = 0d;
                bool nudgeSuccess = false;
                double nudge = 0.00390625d;
                int nudgeDivisions = 0;

                while (nudgeDivisions < MAX_NUDGE_DIVISIONS) {
                    bool success = true;
                    
                    for (int j = 0; j < Program.METRIC_COUNT; j++) {
                        double newMagnitude = magnitudes[j] + nudge * data.MagnitudeVectors[j];

                        if (newMagnitude < 0d) {
                            success = false;
                            
                            break;
                        }
                        
                        newMagnitudes[j] = newMagnitude;
                        metricCoefficients[j] = newMagnitude * data.NormalizedCoefficients[j];
                    }

                    if (success) {
                        CacheResults(dataSet);
                        Table.GenerateComparisonTable(data.ResultsTable, data.Results, dataSet.Size);
                        newCorrelation = Table.Correlation(data.ResultsTable, dataSet.DifficultyComparisons, dataSet.Size);
                        nudgeSuccess = true;

                        if (newCorrelation > correlation)
                            break;
                    }

                    nudge /= 2d;
                    nudgeDivisions++;
                }

                if (nudgeSuccess) {
                    (magnitudes, newMagnitudes) = (newMagnitudes, magnitudes);
                    correlation = newCorrelation;
                }
                else
                    break;
            }

            for (int i = 0; i < Program.METRIC_COUNT; i++)
                metricCurveWeights[i] = magnitudes[i] * CurveWeights.Normalize(metricCurveWeights[i]);

            NormalizeCurveWeights();
            ApplyWeights();
            
            return correlation;
        }

        public double CalculateCorrelation(DataSet dataSet) {
            CacheResults(dataSet);
            Table.GenerateComparisonTable(data.ResultsTable, data.Results, dataSet.Size);
            
            return Table.Correlation(data.ResultsTable, dataSet.DifficultyComparisons, dataSet.Size);
        }

        private void ApplyWeights() {
            for (int i = 0; i < Program.METRIC_COUNT; i++)
                metricCoefficients[i] = new Coefficients(metricCurveWeights[i]);
        }

        private void NormalizeCurveWeights() {
            double sum = 0d;

            foreach (var curveWeights in metricCurveWeights)
                sum += curveWeights.Magnitude;

            for (int i = 0; i < metricCurveWeights.Length; i++)
                metricCurveWeights[i] /= sum;
        }

        private void CacheResults(DataSet dataSet) {
            for (int i = 0; i < dataSet.Size; i++) {
                double[] metricResults = dataSet.Samples[i].Metrics;
                double sum = 0;

                for (int j = 0; j < Program.METRIC_COUNT; j++) {
                    double result = metricResults[j];
                    var coefficients = metricCoefficients[j];
                    
                    sum += result * (coefficients.X1 + result * (coefficients.X2 + result * coefficients.X3));
                }

                data.Results[i] = sum;
            }
        }

        public static void Cross(Random random, Calculator parent1, Calculator parent2, Calculator child) {
            for (int i = 0; i < Program.METRIC_COUNT; i++) {
                if (random.NextDouble() < MUTATION_CHANCE) {
                    child.metricCurveWeights[i] = random.NextDouble() * CurveWeights.Normalize(new CurveWeights(
                        random.NextDouble(),
                        random.NextDouble(),
                        random.NextDouble()));
                }
                else {
                    if (random.NextDouble() > 0.5d)
                        child.metricCurveWeights[i] = parent1.metricCurveWeights[i];
                    else
                        child.metricCurveWeights[i] = parent2.metricCurveWeights[i];
                }
            }
            
            child.NormalizeCurveWeights();
            child.ApplyWeights();
        }
    }
}