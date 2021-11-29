using System.IO;

namespace ChartAutoRating {
    public class Network {
        private int metricCount;
        private Coefficients[,,] valueCoefficients;
        private Coefficients[] weightCoefficients;

        public static Network Create(int metricCount) =>
            new Network {
                metricCount = metricCount,
                valueCoefficients = new Coefficients[metricCount, metricCount, metricCount],
                weightCoefficients = new Coefficients[metricCount]
            };

        public static Network Deserialize(BinaryReader reader) {
            int metricCount = reader.ReadInt32();
            
            var network = new Network {
                metricCount = metricCount,
                valueCoefficients = new Coefficients[metricCount, metricCount, metricCount],
                weightCoefficients = new Coefficients[metricCount]
            };
            
            for (int i = 0; i < metricCount; i++) {
                for (int j = i; j < metricCount; j++) {
                    for (int k = j; k < metricCount; k++) {
                        double x1 = reader.ReadDouble();
                        double x2 = reader.ReadDouble();
                        double x3 = reader.ReadDouble();
                        double x4 = reader.ReadDouble();
                        double x5 = reader.ReadDouble();

                        network.valueCoefficients[i, j, k] = new Coefficients(x1, x2, x3, x4, x5);
                    }
                }
            }
            
            for (int i = 0; i < metricCount; i++) {
                double x1 = reader.ReadDouble();
                double x2 = reader.ReadDouble();
                double x3 = reader.ReadDouble();
                double x4 = reader.ReadDouble();
                double x5 = reader.ReadDouble();

                network.weightCoefficients[i] = new Coefficients(x1, x2, x3, x4, x5);
            }

            return network;
        }

        public void Serialize(BinaryWriter writer) {
            writer.Write(metricCount);

            for (int i = 0; i < metricCount; i++) {
                for (int j = i; j < metricCount; j++) {
                    for (int k = j; k < metricCount; k++) {
                        var coeff = valueCoefficients[i, j, k];

                        writer.Write(coeff.X1);
                        writer.Write(coeff.X2);
                        writer.Write(coeff.X3);
                    }
                }
            }
            
            for (int i = 0; i < metricCount; i++) {
                var coeff = weightCoefficients[i];

                writer.Write(coeff.X1);
                writer.Write(coeff.X2);
                writer.Write(coeff.X3);
            }
        }

        public void SetValueCoefficients(int indexA, int indexB, int indexC, Coefficients metricCoefficients) => valueCoefficients[indexA, indexB, indexC] = metricCoefficients;

        public void SetWeightCoefficients(int metricIndex, Coefficients metricCoefficients) => weightCoefficients[metricIndex] = metricCoefficients;

        public double GetValue(Data data) {
            double sumValue = 0d;
            double sumWeight = 0d;

            foreach (var sample in data.DataSamples) {
                double value = 0d;
                double weight = 0d;
                
                for (int i = 0; i < metricCount; i++) {
                    double a = sample.Values[i];

                    a = a * a * a;
                    weight += Coefficients.Compute(a, weightCoefficients[i]);
                }

                weight *= sample.Weight;
                
                for (int i = 0; i < metricCount; i++) {
                    double a = sample.Values[i];

                    for (int j = i; j < metricCount; j++) {
                        double b = sample.Values[j];

                        for (int k = j; k < metricCount; k++) {
                            double abc = a * b * sample.Values[k];

                            value += Coefficients.Compute(abc, valueCoefficients[i, j, k]);
                        }
                    }
                }

                sumValue += weight * value;
                sumWeight += weight;
            }

            return sumValue / sumWeight;
        }
    }
}