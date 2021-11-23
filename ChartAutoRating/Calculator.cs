using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartAutoRating {
    public partial class Calculator {
        private static readonly int MAX_NUDGE_REDUCTIONS = 16;

        public Coefficients[] MetricCoefficients { get; private set; }
        
        private float[] cachedResults;
        private Table cachedResultsTable;

        public Calculator(int size) {
            MetricCoefficients = new Coefficients[Program.METRIC_COUNT];
            cachedResults = new float[size];
            cachedResultsTable = new Table(size);
        }

        public void ApplyWeights(CurveWeights[] weights) {
            for (int i = 0; i < Program.METRIC_COUNT; i++)
                MetricCoefficients[i] = new Coefficients(weights[i]);
        }

        public void Maximize(DataSet dataSet) {
            float[] magnitudeVectors = new float[Program.METRIC_COUNT];
            float[] magnitudes = MetricCoefficients.Select(c => c.Magnitude).ToArray();
            float[] newMagnitudes = new float[Program.METRIC_COUNT];
            var normalizedCoefficients = MetricCoefficients.Select(Coefficients.Normalize).ToArray();
            var vectorTable = new Table(dataSet.Size);
            float correlation = 0f;
            float nudge = 0.125f;
            int nudgeReductions = 0;
            
            CacheResults(dataSet);
            Table.GenerateComparisonTable(cachedResultsTable, index => cachedResults[index], dataSet.Size);

            for (int i = 0; i < 65536; i++) {
                Table.Compare(vectorTable, dataSet.DifficultyComparisons, cachedResultsTable, dataSet.Size);
                
                for (int j = 0; j < Program.METRIC_COUNT; j++)
                    magnitudeVectors[j] = Table.Correlation(dataSet.MetricComparisons[j], vectorTable, dataSet.Size);

                float newCorrelation;

                do {
                    for (int j = 0; j < Program.METRIC_COUNT; j++) {
                        float newMagnitude = magnitudes[j] + nudge * magnitudeVectors[j];
                        
                        newMagnitudes[j] = newMagnitude;
                        MetricCoefficients[j] = newMagnitude * normalizedCoefficients[j];
                    }
                    
                    CacheResults(dataSet);
                    Table.GenerateComparisonTable(cachedResultsTable, index => cachedResults[index], dataSet.Size);
                    newCorrelation = Table.Correlation(cachedResultsTable, dataSet.DifficultyComparisons, dataSet.Size);

                    if (newCorrelation > correlation)
                        break;
                    
                    nudge /= 2f;
                    nudgeReductions++;
                } while (nudgeReductions < MAX_NUDGE_REDUCTIONS);
                
                if (nudgeReductions >= MAX_NUDGE_REDUCTIONS)
                    break;

                (magnitudes, newMagnitudes) = (newMagnitudes, magnitudes);
                correlation = newCorrelation;
            }
            
            Normalize();
        }

        public void CalculateResults(DataSet dataSet, float[] results, out float correlation) {
            CacheResults(dataSet);
            Table.GenerateComparisonTable(cachedResultsTable, index => cachedResults[index], dataSet.Size);
            correlation = Table.Correlation(cachedResultsTable, dataSet.DifficultyComparisons, dataSet.Size);
            
            if (results != null)
                Array.Copy(cachedResults, results, dataSet.Size);
        }

        private void CacheResults(DataSet dataSet) {
            for (int i = 0; i < dataSet.Size; i++) {
                float[] metricResults = dataSet.Samples[i].Metrics;
                float sum = 0;

                for (int j = 0; j < Program.METRIC_COUNT; j++) {
                    float result = metricResults[j];
                    var coefficients = MetricCoefficients[j];
                    
                    sum += result * (coefficients.X1 + result * (coefficients.X2 + result * coefficients.X3));
                }

                cachedResults[i] = sum;
            }
        }

        private void Normalize() {
            float sum = 0f;

            foreach (var coefficients in MetricCoefficients)
                sum += coefficients.Magnitude;

            for (int i = 0; i < MetricCoefficients.Length; i++)
                MetricCoefficients[i] /= sum;
        }
    }
}