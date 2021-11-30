﻿using System;
using System.IO;
using System.Threading.Tasks;
using ChartAutoRating;
using ChartMetrics;

namespace ChartRatingTrainer {
    public class Calculator {
        public static readonly int METRIC_COUNT = ChartProcessor.Metrics.Count;
        public static readonly double OVERWEIGHT_THRESHOLD_VALUE = 0.06d;
        public static readonly double OVERWEIGHT_THRESHOLD_WEIGHT = 0.4d;
        
        private static readonly double MUTATION_CHANCE = 0.025d;
        private static readonly double MUTATION_AMOUNT_VALUE = 0.001875d;
        private static readonly double MUTATION_AMOUNT_WEIGHT = 0.0125d;
        private static readonly double OVERWEIGHT_BIAS = 0.25d;

        public Curve[,,] ValueCurves { get; }
        
        public Curve[] WeightCurves { get; }

        private Network network;

        private Calculator() {
            network = Network.Create(METRIC_COUNT);
            ValueCurves = new Curve[METRIC_COUNT, METRIC_COUNT, METRIC_COUNT];
            WeightCurves = new Curve[METRIC_COUNT];
        }

        public static Calculator Random(Random random) {
            var calculator = new Calculator();
            
            for (int i = 0; i < METRIC_COUNT; i++) {
                for (int j = i; j < METRIC_COUNT; j++) {
                    for (int k = j; k < METRIC_COUNT; k++)
                        calculator.ValueCurves[i, j, k] = Curve.Random(random, OVERWEIGHT_THRESHOLD_VALUE * random.NextDouble());
                }
                
                calculator.WeightCurves[i] = Curve.Random(random, OVERWEIGHT_THRESHOLD_WEIGHT * random.NextDouble());
            }

            calculator.ApplyCurves();

            return calculator;
        }

        public static Calculator Deserialize(BinaryReader reader) {
            var calculator = new Calculator();

            for (int i = 0; i < METRIC_COUNT; i++) {
                for (int j = i; j < METRIC_COUNT; j++) {
                    for (int k = j; k < METRIC_COUNT; k++) {
                        double w0 = reader.ReadDouble();
                        double w1 = reader.ReadDouble();
                        double w2 = reader.ReadDouble();
                        double w3 = reader.ReadDouble();
                        double w4 = reader.ReadDouble();
                        double w5 = reader.ReadDouble();

                        calculator.ValueCurves[i, j, k] = new Curve(w0, w1, w2, w3, w4, w5);
                    }
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
                    for (int k = j; k < METRIC_COUNT; k++) {
                        var curve = ValueCurves[i, j, k];
                    
                        writer.Write(curve.A);
                        writer.Write(curve.B);
                        writer.Write(curve.C);
                        writer.Write(curve.D);
                        writer.Write(curve.E);
                        writer.Write(curve.F);
                    }
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
            double[] resultValues = dataSet.ResultValues;
            double[] resultPositions = dataSet.ResultPositions;

            Parallel.For(0, dataSet.Size, i => resultValues[i] = network.GetValue(dataSet.Datas[i]));

            double min = double.PositiveInfinity;
            double max = 0d;

            foreach (double value in resultValues) {
                if (value < min)
                    min = value;

                if (value > max)
                    max = value;
            }

            double scale = 1d / (max - min);

            for (int i = 0; i < dataSet.Size; i++)
                resultPositions[i] = scale * (resultValues[i] - min);
        }

        public double CalculateFitness(DataSet[] dataSets) {
            double max = 0d;
            double sum = 0d;
            int count = 0;

            foreach (var dataSet in dataSets) {
                double[] resultPositions = dataSet.ResultPositions;
                double[] dataPositions = dataSet.PositionValues;
                    
                CacheResults(dataSet);

                for (int i = 0; i < dataSet.Size; i++) {
                    double value = resultPositions[i] - dataPositions[i];
                    
                    value *= value;
                    
                    if (value > max)
                        max = value;
                    
                    sum += value;
                }

                count += dataSet.Size;
            }

            double overWeight = 0d;

            for (int i = 0; i < METRIC_COUNT; i++) {
                double value;
                
                for (int j = i; j < METRIC_COUNT; j++) {
                    for (int k = j; k < METRIC_COUNT; k++) {
                        value = ValueCurves[i, j, k].Magnitude;
                        
                        if (value > OVERWEIGHT_THRESHOLD_VALUE)
                            overWeight += value - OVERWEIGHT_THRESHOLD_VALUE;
                    }
                }

                value = WeightCurves[i].Magnitude;

                if (value > OVERWEIGHT_THRESHOLD_WEIGHT)
                    overWeight += value - OVERWEIGHT_THRESHOLD_WEIGHT;
            }

            return (1d - Math.Sqrt(max)) * (1d - Math.Sqrt(sum / count)) - OVERWEIGHT_BIAS * overWeight;
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
                    for (int k = 0; k < METRIC_COUNT; k++) {
                        childValueCurves[i, j, k] = 0.5d * (parentValueCurves1[i, j, k] + parentValueCurves2[i, j, k]);

                        if (random.NextDouble() < MUTATION_CHANCE)
                            parentValueCurves2[i, j, k] = Curve.Clamp(parentValueCurves2[i, j, k] + Curve.Random(random, MUTATION_AMOUNT_VALUE * (2d * random.NextDouble() - 1d)));
                    }
                }

                childWeightCurves[i] = 0.5d * (parentWeightCurves1[i] + parentWeightCurves2[i]);

                if (random.NextDouble() < MUTATION_CHANCE)
                    parentWeightCurves2[i] = Curve.Clamp(parentWeightCurves2[i] + Curve.Random(random, MUTATION_AMOUNT_WEIGHT * (2d * random.NextDouble() - 1d)));
            }

            parent2.ApplyCurves();
            child.ApplyCurves();
        }

        private void ApplyCurves() {
            double totalMagnitude = 0d;
            
            for (int i = 0; i < METRIC_COUNT; i++) {
                for (int j = i; j < METRIC_COUNT; j++) {
                    for (int k = j; k < METRIC_COUNT; k++)
                        totalMagnitude += ValueCurves[i, j, k].Magnitude;
                }
            }

            double scale = 1d / totalMagnitude;
            
            for (int i = 0; i < METRIC_COUNT; i++) {
                for (int j = i; j < METRIC_COUNT; j++) {
                    for (int k = j; k < METRIC_COUNT; k++) {
                        ValueCurves[i, j, k] = scale * ValueCurves[i, j, k];
                        network.SetValueCoefficients(i, j, k, ValueCurves[i, j, k].ToCoefficients());
                    }
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