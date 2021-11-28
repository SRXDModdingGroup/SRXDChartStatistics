using System.IO;

namespace ChartAutoRating {
    public class Network {
        private int metricCount;
        private Coefficients[,] valueCoefficients;
        private Coefficients[] weightCoefficients;

        public static Network Create(int metricCount) =>
            new Network {
                metricCount = metricCount,
                valueCoefficients = new Coefficients[metricCount, metricCount],
                weightCoefficients = new Coefficients[metricCount]
            };

        public static Network Deserialize(BinaryReader reader) {
            int metricCount = reader.ReadInt32();
            
            var network = new Network {
                metricCount = metricCount,
                valueCoefficients = new Coefficients[metricCount, metricCount],
                weightCoefficients = new Coefficients[metricCount]
            };
            
            for (int i = 0; i < metricCount; i++) {
                for (int j = i; j < metricCount; j++) {
                    double x1 = reader.ReadDouble();
                    double x2 = reader.ReadDouble();
                    double x3 = reader.ReadDouble();

                    network.valueCoefficients[i, j] = new Coefficients(x1, x2, x3);
                }
            }
            
            for (int i = 0; i < metricCount; i++) {
                double x1 = reader.ReadDouble();
                double x2 = reader.ReadDouble();
                double x3 = reader.ReadDouble();

                network.weightCoefficients[i] = new Coefficients(x1, x2, x3);
            }

            return network;
        }

        public void Serialize(BinaryWriter writer) {
            writer.Write(metricCount);

            for (int i = 0; i < metricCount; i++) {
                for (int j = i; j < metricCount; j++) {
                    var coeff = valueCoefficients[i, j];

                    writer.Write(coeff.X1);
                    writer.Write(coeff.X2);
                    writer.Write(coeff.X3);
                }
            }
            
            for (int i = 0; i < metricCount; i++) {
                var coeff = weightCoefficients[i];

                writer.Write(coeff.X1);
                writer.Write(coeff.X2);
                writer.Write(coeff.X3);
            }
        }

        public void SetValueCoefficients(int indexA, int indexB, Coefficients metricCoefficients) => valueCoefficients[indexA, indexB] = metricCoefficients;

        public void SetWeightCoefficients(int metricIndex, Coefficients metricCoefficients) => weightCoefficients[metricIndex] = metricCoefficients;

        public double GetValue(Data data) {
            double sumValue = 0d;
            double sumWeight = 0d;

            foreach (var sample in data.DataSamples) {
                double value = 0d;
                double weight = 0d;
                
                for (int i = 0; i < metricCount; i++) {
                    double a = sample.Values[i];
                    var coeff = weightCoefficients[i];

                    weight += a * (coeff.X1 + a * (coeff.X2 + a * coeff.X3));
                }

                weight *= sample.Weight;
                
                for (int i = 0; i < metricCount; i++) {
                    double a = sample.Values[i];

                    for (int j = i; j < metricCount; j++) {
                        double ab = a * sample.Values[j];
                        var coeff = valueCoefficients[i, j];

                        value += ab * (coeff.X1 + ab * (coeff.X2 + ab * coeff.X3));
                    }
                }

                sumValue += weight * value;
                sumWeight += weight;
            }

            return sumValue / sumWeight;
        }
    }
}