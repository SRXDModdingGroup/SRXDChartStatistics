namespace ChartAutoRating {
    public class Network {
        public Coefficients[] Coefficients { get; }

        public Network(int metricCount) {
            Coefficients = new Coefficients[metricCount];
        }

        public double GetValue(Data data) {
            var dataSamples = data.DataSamples;
            double sum = 0d;
            
            for (int i = 0; i < Coefficients.Length; i++) {
                var coeff = Coefficients[i];
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