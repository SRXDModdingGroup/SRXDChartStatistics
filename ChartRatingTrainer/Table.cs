using System;

namespace ChartRatingTrainer {
    public static class Table {
        // private double[,] data;
        // private double scale;
        //
        // public Table(int size) {
        //     data = new double[size, size];
        // }
        //
        // private double this[int row, int column] {
        //     get => data[row, column];
        //     set => data[row, column] = value;
        // }
        //
        // public double SumOfRow(int row, int size) {
        //     double sum = 0d;
        //     
        //     for (int column = 0; column < size; column++) {
        //         if (column >= row)
        //             sum += this[row, column];
        //         else
        //             sum += this[column, row];
        //     }
        //
        //     return sum;
        // }
        //
        // public double MeanOfRow(int row, int size) {
        //     double sum = 0d;
        //     double absSum = 0d;
        //     
        //     for (int column = 0; column < size; column++) {
        //         double value;
        //         
        //         if (column >= row)
        //             value = this[row, column];
        //         else
        //             value = this[column, row];
        //
        //         sum += value;
        //         absSum += Math.Abs(value);
        //     }
        //
        //     return sum / absSum;
        // }
        //
        // public static void GenerateComparisonTable(Table target, double[] data, double midpoint, int size) {
        //     double sum = 0d;
        //     
        //     for (int i = 0; i < size - 1; i++) {
        //         for (int j = i + 1; j < size; j++) {
        //             double val = data[i] - data[j];
        //
        //             val /= Math.Abs(val) + midpoint;
        //
        //             if (double.IsNaN(val))
        //                 val = 0d;
        //             
        //             target[i, j] = val;
        //             sum += val;
        //         }
        //     }
        //
        //     target.scale = 1d / (size * (size + 1) / 2);
        // }
        //
        // public static double Covariance(Table a, Table b, int size) {
        //     double sum = 0d;
        //     
        //     for (int i = 0; i < size - 1; i++) {
        //         for (int j = i + 1; j < size; j++)
        //             sum += a[i, j] * b[i, j];
        //     }
        //
        //     return a.scale * sum;
        // }
        //
        // public static double CovarianceForRow(Table a, Table b, int row, int size) {
        //     double sum = 0d;
        //
        //     for (int column = 0; column < size; column++) {
        //         if (column >= row)
        //             sum += a[row, column] * b[row, column];
        //         else
        //             sum += a[column, row] * b[column, row];
        //     }
        //     
        //     return sum / size;
        // }
    }
}