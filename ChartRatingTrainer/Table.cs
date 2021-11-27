using System;

namespace ChartRatingTrainer {
    public class Table {
        private double[,] data;

        public Table(int size) {
            data = new double[size, size];
        }

        public Table(Table table) {
            data = (double[,]) table.data.Clone();
        }

        private double this[int row, int column] {
            get => data[row, column];
            set => data[row, column] = value;
        }

        public static void Compare(Table target, Table a, Table b, int size) {
            for (int i = 0; i < size - 1; i++) {
                for (int j = i + 1; j < size; j++)
                    target[i, j] = a[i, j].CompareTo(b[i, j]);
            }
        }

        public static void GenerateComparisonTable(Table target, double[] data, int size) {
            for (int i = 0; i < size - 1; i++) {
                for (int j = i + 1; j < size; j++)
                    target[i, j] = data[i].CompareTo(data[j]);
            }
        }
        
        public static void CorrelationComponents(Table a, Table b, int size, out double sum, out double absSum) {
            sum = 0d;
            absSum = 0d;
            
            for (int i = 0; i < size - 1; i++) {
                for (int j = i + 1; j < size; j++) {
                    double product = Math.Sign(a[i, j] * b[i, j]);

                    sum += product;
                    absSum += Math.Abs(product);
                }
            }
        }

        public static double Correlation(Table a, Table b, int size) {
            double sum = 0d;
            double absSum = 0d;
            
            for (int i = 0; i < size - 1; i++) {
                for (int j = i + 1; j < size; j++) {
                    double product = Math.Sign(a[i, j] * b[i, j]);

                    sum += product;
                    absSum += Math.Abs(product);
                }
            }

            if (absSum == 0d)
                return 0d;

            return sum / absSum;
        }

        public static double CorrelationForRow(Table a, Table b, int row, int size) {
            double sum = 0d;
            double absSum = 0d;
            
            for (int column = 0; column < size; column++) {
                if (column == row)
                    continue;

                double value;

                if (column > row)
                    value = a[row, column] * b[row, column];
                else
                    value = a[column, row] * b[column, row];

                sum += value;
                absSum += Math.Abs(value);
            }
            
            return sum / absSum;
        }
    }
}