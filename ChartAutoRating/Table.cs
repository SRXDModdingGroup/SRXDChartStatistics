using System;
using System.Globalization;
using System.Security.Cryptography;

namespace ChartAutoRating {
    public class Table {
        private float[,] data;

        public Table(int size) {
            data = new float[size, size];
        }

        public Table(Table table) {
            data = (float[,]) table.data.Clone();
        }

        private float this[int row, int column] {
            get => data[row, column];
            set => data[row, column] = value;
        }

        public static void Compare(Table target, Table a, Table b, int size) {
            for (int i = 0; i < size - 1; i++) {
                for (int j = i + 1; j < size; j++)
                    target[i, j] = a[i, j].CompareTo(b[i, j]);
            }
        }

        public static void GenerateComparisonTable(Table target, Func<int, float> selector, int size) {
            for (int i = 0; i < size - 1; i++) {
                for (int j = i + 1; j < size; j++)
                    target[i, j] = selector(i).CompareTo(selector(j));
            }
        }

        public static float Correlation(Table a, Table b, int size) {
            float sum = 0f;
            float absSum = 0f;
            
            for (int i = 0; i < size - 1; i++) {
                for (int j = i + 1; j < size; j++) {
                    float product = Math.Sign(a[i, j] * b[i, j]);

                    sum += product;
                    absSum += Math.Abs(product);
                }
            }

            if (absSum == 0f)
                return 0f;

            return sum / absSum;
        }
    }
}