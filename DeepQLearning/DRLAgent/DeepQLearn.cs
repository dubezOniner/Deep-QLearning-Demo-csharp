using ConvnetSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepQLearning.DRLAgent
{
    // An agent is in state0 and does action0
    // environment then assigns reward0 and provides new state, state1
    // Experience nodes store all this information, which is used in the
    // Q-learning update step
    [Serializable]
    public class Experience
    {
        public double[] state0;
        public int action0;
        public double reward0;
        public double[] state1;

        public Experience()
        {

        }

        public Experience(double[] state0, int action0, double reward0, double[] state1)
        {
            this.state0 = state0;
            this.action0 = action0;
            this.reward0 = reward0;
            this.state1 = state1;
        }
    }

    [Serializable]
    public struct Action
    {
        public int action;
        public double value;
    };

    // A Brain object does all the magic.
    // over time it receives some inputs and some rewards
    // and its job is to set the outputs to maximize the expected reward
    [Serializable]
    public class DeepQLearn
    {
        TrainingOptions opt;

        int temporal_window;
        int experience_size;
        double start_learn_threshold;
        double gamma;
        double learning_steps_total;
        double learning_steps_burnin;
        double epsilon_min;
        public double epsilon_test_time;

        int net_inputs;
        int num_states;
        int num_actions;
        int window_size;
        List<Volume> state_window;
        List<int> action_window;
        List<double> reward_window;
        List<double[]> net_window;

        double age;
        double forward_passes;
        public double epsilon;
        double latest_reward;
        Volume last_input;
        TrainingWindow average_reward_window;
        TrainingWindow average_loss_window;
        public bool learning;

        Net value_net;
        public Trainer tdtrainer;

        Util util;

        List<double> random_action_distribution;
        List<Experience> experience;

        public DeepQLearn(int num_states, int num_actions, TrainingOptions opt)
        {
            this.util = new Util();
            this.opt = opt;

            // in number of time steps, of temporal memory
            // the ACTUAL input to the net will be (x,a) temporal_window times, and followed by current x
            // so to have no information from previous time step going into value function, set to 0.
            this.temporal_window = opt.temporal_window != int.MinValue ? opt.temporal_window : 1;
            // size of experience replay memory
            this.experience_size = opt.experience_size != int.MinValue ? opt.experience_size : 30000;
            // number of examples in experience replay memory before we begin learning
            this.start_learn_threshold = opt.start_learn_threshold != double.MinValue ? opt.start_learn_threshold : Math.Floor(Math.Min(this.experience_size * 0.1, 1000));
            // gamma is a crucial parameter that controls how much plan-ahead the agent does. In [0,1]
            this.gamma = opt.gamma != double.MinValue ? opt.gamma : 0.8;

            // number of steps we will learn for
            this.learning_steps_total = opt.learning_steps_total != int.MinValue ? opt.learning_steps_total : 100000;
            // how many steps of the above to perform only random actions (in the beginning)?
            this.learning_steps_burnin = opt.learning_steps_burnin != int.MinValue ? opt.learning_steps_burnin : 3000;
            // what epsilon value do we bottom out on? 0.0 => purely deterministic policy at end
            this.epsilon_min = opt.epsilon_min != double.MinValue ? opt.epsilon_min : 0.05;
            // what epsilon to use at test time? (i.e. when learning is disabled)
            this.epsilon_test_time = opt.epsilon_test_time != double.MinValue ? opt.epsilon_test_time : 0.00;

            // advanced feature. Sometimes a random action should be biased towards some values
            // for example in flappy bird, we may want to choose to not flap more often
            if (opt.random_action_distribution != null)
            {
                // this better sum to 1 by the way, and be of length this.num_actions
                this.random_action_distribution = opt.random_action_distribution;
                if (this.random_action_distribution.Count != num_actions)
                {
                    Console.WriteLine("TROUBLE. random_action_distribution should be same length as num_actions.");
                }

                var sum_of_dist = this.random_action_distribution.Sum();
                if (Math.Abs(sum_of_dist - 1.0) > 0.0001) { Console.WriteLine("TROUBLE. random_action_distribution should sum to 1!"); }
            }
            else
            {
                this.random_action_distribution = new List<double>();
            }

            // states that go into neural net to predict optimal action look as
            // x0,a0,x1,a1,x2,a2,...xt
            // this variable controls the size of that temporal window. Actions are
            // encoded as 1-of-k hot vectors
            this.net_inputs = num_states * this.temporal_window + num_actions * this.temporal_window + num_states;
            this.num_states = num_states;
            this.num_actions = num_actions;
            this.window_size = Math.Max(this.temporal_window, 2); // must be at least 2, but if we want more context even more
            this.state_window = new List<Volume>();
            this.action_window = new List<int>();
            this.reward_window = new List<double>();
            this.net_window = new List<double[]>();

            // Init wth dummy data
            for (int i = 0; i < window_size; i++) this.state_window.Add(new Volume(1, 1, 1));
            for (int i = 0; i < window_size; i++) this.action_window.Add(0);
            for (int i = 0; i < window_size; i++) this.reward_window.Add(0.0);
            for (int i = 0; i < window_size; i++) this.net_window.Add(new double[] { 0.0 });

            // create [state -> value of all possible actions] modeling net for the value function
            var layer_defs = new List<LayerDefinition>();
            if (opt.layer_defs != null)
            {
                // this is an advanced usage feature, because size of the input to the network, and number of
                // actions must check out. This is not very pretty Object Oriented programming but I can't see
                // a way out of it :(
                layer_defs = opt.layer_defs;
                if (layer_defs.Count < 2) { Console.WriteLine("TROUBLE! must have at least 2 layers"); }
                if (layer_defs[0].type != "input") { Console.WriteLine("TROUBLE! first layer must be input layer!"); }
                if (layer_defs[layer_defs.Count - 1].type != "regression") { Console.WriteLine("TROUBLE! last layer must be input regression!"); }
                if (layer_defs[0].out_depth * layer_defs[0].out_sx * layer_defs[0].out_sy != this.net_inputs)
                {
                    Console.WriteLine("TROUBLE! Number of inputs must be num_states * temporal_window + num_actions * temporal_window + num_states!");
                }
                if (layer_defs[layer_defs.Count - 1].num_neurons != this.num_actions)
                {
                    Console.WriteLine("TROUBLE! Number of regression neurons should be num_actions!");
                }
            }
            else
            {
                // create a very simple neural net by default
                layer_defs.Add(new LayerDefinition { type = "input", out_sx = 1, out_sy = 1, out_depth = this.net_inputs });
                if (opt.hidden_layer_sizes != null)
                {
                    // allow user to specify this via the option, for convenience
                    var hl = opt.hidden_layer_sizes;
                    for (var k = 0; k < hl.Length; k++)
                    {
                        layer_defs.Add(new LayerDefinition { type = "fc", num_neurons = hl[k], activation = "relu" }); // relu by default
                    }
                }
            }

            // Create the network
            this.value_net = new Net();
            this.value_net.makeLayers(layer_defs);

            // and finally we need a Temporal Difference Learning trainer!
            var options = new Options { learning_rate = 0.01, momentum = 0.0, batch_size = 64, l2_decay = 0.01 };
            if (opt.options != null)
            {
                options = opt.options; // allow user to overwrite this
            }

            this.tdtrainer = new Trainer(this.value_net, options);

            // experience replay
            this.experience = new List<Experience>();

            // various housekeeping variables
            this.age = 0; // incremented every backward()
            this.forward_passes = 0; // incremented every forward()
            this.epsilon = 1.0; // controls exploration exploitation tradeoff. Should be annealed over time
            this.latest_reward = 0;
            //this.last_input = [];
            this.average_reward_window = new TrainingWindow(1000, 10);
            this.average_loss_window = new TrainingWindow(1000, 10);
            this.learning = true;
        }

        public int random_action()
        {
            // a bit of a helper function. It returns a random action
            // we are abstracting this away because in future we may want to 
            // do more sophisticated things. For example some actions could be more
            // or less likely at "rest"/default state.

            int action = util.randi(0, this.num_actions);

            if (this.random_action_distribution.Count != 0)
            {
                // okay, lets do some fancier sampling:
                var p = util.randf(0, 1.0);
                var cumprob = 0.0;
                for (var k = 0; k < this.num_actions; k++)
                {
                    cumprob += this.random_action_distribution[k];
                    if (p < cumprob) { action = k; break; }
                }
            }

            return action;
        }

        public Action policy(double[] s)
        {
            // compute the value of doing any action in this state
            // and return the argmax action and its value
            var svol = new Volume(1, 1, this.net_inputs);
            svol.w = s;
            var action_values = this.value_net.forward(svol, false);
            var maxk = 0;
            var maxval = action_values.w[0];
            for (var k = 1; k < this.num_actions; k++)
            {
                if (action_values.w[k] > maxval) { maxk = k; maxval = action_values.w[k]; }
            }
            return new Action { action = maxk, value = maxval };
        }

        public double[] getNetInput(Volume xt)
        {
            // return s = (x,a,x,a,x,a,xt) state vector. 
            // It's a concatenation of last window_size (x,a) pairs and current state x
            List<double> w = new List<double>();

            // start with current state and now go backwards and append states and actions from history temporal_window times
            w.AddRange(xt.w);

            var n = this.window_size;
            for (var k = 0; k < this.temporal_window; k++)
            {
                // state
                w.AddRange(this.state_window[n - 1 - k].w);
                // action, encoded as 1-of-k indicator vector. We scale it up a bit because
                // we dont want weight regularization to undervalue this information, as it only exists once
                var action1ofk = new double[this.num_actions];
                for (var q = 0; q < this.num_actions; q++) action1ofk[q] = 0.0;
                action1ofk[this.action_window[n - 1 - k]] = 1.0 * this.num_states;
                w.AddRange(action1ofk);
            }

            return w.ToArray();
        }

        public int forward(Volume input_array)
        {
            // compute forward (behavior) pass given the input neuron signals from body
            this.forward_passes += 1;
            this.last_input = input_array; // back this up

            // create network input
            int action;
            double[] net_input;
            if (this.forward_passes > this.temporal_window)
            {
                // we have enough to actually do something reasonable
                net_input = this.getNetInput(input_array);
                if (this.learning)
                {
                    // compute epsilon for the epsilon-greedy policy
                    this.epsilon = Math.Min(1.0, Math.Max(this.epsilon_min, 1.0 - (this.age - this.learning_steps_burnin) / (this.learning_steps_total - this.learning_steps_burnin)));
                }
                else
                {
                    this.epsilon = this.epsilon_test_time; // use test-time value
                }

                var rf = util.randf(0, 1);
                if (rf < this.epsilon)
                {
                    // choose a random action with epsilon probability
                    action = this.random_action();
                }
                else
                {
                    // otherwise use our policy to make decision
                    var maxact = this.policy(net_input);
                    action = maxact.action;
                }
            }
            else
            {
                // pathological case that happens first few iterations 
                // before we accumulate window_size inputs
                net_input = new List<double>().ToArray();
                action = this.random_action();
            }

            // remember the state and action we took for backward pass
            this.net_window.RemoveAt(0);
            this.net_window.Add(net_input);
            this.state_window.RemoveAt(0);
            this.state_window.Add(input_array);
            this.action_window.RemoveAt(0);
            this.action_window.Add(action);

            return action;
        }

        public void backward(double reward)
        {
            this.latest_reward = reward;
            this.average_reward_window.add(reward);

            this.reward_window.RemoveAt(0);
            this.reward_window.Add(reward);

            if (!this.learning) { return; }

            // various book-keeping
            this.age += 1;

            // it is time t+1 and we have to store (s_t, a_t, r_t, s_{t+1}) as new experience
            // (given that an appropriate number of state measurements already exist, of course)
            if (this.forward_passes > this.temporal_window + 1)
            {
                var e = new Experience();
                var n = this.window_size;
                e.state0 = this.net_window[n - 2];
                e.action0 = this.action_window[n - 2];
                e.reward0 = this.reward_window[n - 2];
                e.state1 = this.net_window[n - 1];

                if (this.experience.Count < this.experience_size)
                {
                    this.experience.Add(e);
                }
                else
                {
                    // replace. finite memory!
                    var ri = util.randi(0, this.experience_size);
                    this.experience[ri] = e;
                }
            }

            // learn based on experience, once we have some samples to go on
            // this is where the magic happens...
            if (this.experience.Count > this.start_learn_threshold)
            {
                var avcost = 0.0;
                for (var k = 0; k < this.tdtrainer.batch_size; k++)
                {
                    var re = util.randi(0, this.experience.Count);
                    var e = this.experience[re];
                    var x = new Volume(1, 1, this.net_inputs);
                    x.w = e.state0;
                    var maxact = this.policy(e.state1);
                    var r = e.reward0 + this.gamma * maxact.value;

                    var ystruct = new Entry { dim=e.action0, val=r};
                    var loss = this.tdtrainer.train(x, ystruct);
                    avcost += double.Parse(loss["loss"]);
                }

                avcost = avcost / this.tdtrainer.batch_size;
                this.average_loss_window.add(avcost);
            }
        }

        public string visSelf()
        {
            var t = "";
            t += "experience replay size: " + this.experience.Count + Environment.NewLine;
            t += "exploration epsilon: " + this.epsilon + Environment.NewLine;
            t += "age: " + this.age + Environment.NewLine;
            t += "average Q-learning loss: " + this.average_loss_window.get_average() + Environment.NewLine;
            t += "smooth-ish reward: " + this.average_reward_window.get_average() + Environment.NewLine;

            return t;
        }
    }
}
