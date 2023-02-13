using System.Windows.Forms;

namespace ChartStatistics; 

public partial class Form1 : Form {
    public Form1() {
        InitializeComponent();
        _ = new ChartView(0.01f, 0.4f, 0.6f, 0.99f, new GraphicsPanel(chartViewPanel));
    }
}