using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConvnetSharp
{
    [Serializable]
    public class TrainingOptions
    {
        public int temporal_window = int.MinValue;
        public int experience_size = int.MinValue;
        public int start_learn_threshold = int.MinValue;
        public int learning_steps_total = int.MinValue;
        public int learning_steps_burnin = int.MinValue;
        public int[] hidden_layer_sizes;

        public double gamma = double.MinValue;
        public double learning_rate = double.MinValue;
        public double epsilon_min = double.MinValue;
        public double epsilon_test_time = double.MinValue;

        public Options options;
        public List<LayerDefinition> layer_defs;
        public List<double> random_action_distribution;
    }
}
