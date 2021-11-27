namespace ChartAutoRating {
    public class Network {
        private Coefficients[] coefficients;

        public Network(int metricCount) {
            coefficients = new Coefficients[metricCount];
        }

        public void SetCoefficients(int metricIndex, Coefficients metricCoefficients) => coefficients[metricIndex] = metricCoefficients;

        public double GetValue(Data data) {
            var dataSamples = data.DataSamples;
            double sum = 0d;
            
            for (int i = 0; i < coefficients.Length; i++) {
                var coeff = coefficients[i];
                var samples = dataSamples[i];

                foreach (var sample in samples) {
                    double value = sample.Value;

                    sum += sample.Weight * value * (coeff.X1 + value * (coeff.X2 + value * coeff.X3));
                }
            }

            return sum;
        }
    }
}