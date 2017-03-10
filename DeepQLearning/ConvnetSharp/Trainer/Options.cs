using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConvnetSharp
{
    [Serializable]
    public class Options
    {
        public string method = string.Empty;
        public int batch_size = int.MinValue;

        public double learning_rate = double.MinValue;
        public double l1_decay = double.MinValue;
        public double l2_decay = double.MinValue;
        public double momentum = double.MinValue;
        public double beta1 = double.MinValue;
        public double beta2 = double.MinValue;
        public double ro = double.MinValue;
        public double eps = double.MinValue;
    }
}
