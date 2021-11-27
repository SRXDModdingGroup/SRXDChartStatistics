namespace ChartAutoRating {
    public class Network {
        private int metricCount;
        private Coefficients[,] valueCoefficients;
        private Coefficients[] weightCoefficients;

        public Network(int metricCount) {
            this.metricCount = metricCount;
            valueCoefficients = new Coefficients[metricCount, metricCount];
            weightCoefficients = new Coefficients[metricCount];
        }

        public void SetValueCoefficients(int indexA, int indexB, Coefficients metricCoefficients) => valueCoefficients[indexA, indexB] = metricCoefficients;

        public double GetValue(Data data) {
            double sum = 0d;

            foreach (var sample in data.DataSamples) {
                for (int i = 0; i < metricCount; i++) {
                    double value = sample.Values[i];
                    
                    for (int j = i; j < metricCount; j++) {
                        double prod = value * sample.Values[j];
                        var coeff = valueCoefficients[i, j];

                        sum += sample.Weight * prod * (coeff.X1 + prod * (coeff.X2 + prod * coeff.X3));
                    }
                }
            }

            return sum;
        }
    }
}