using ConvnetSharp;
using DeepQLearning.DRLAgent;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DeepQLearning
{
    public partial class Form1 : Form
    {
        Pen blackPen = new Pen(Color.Black);
        Pen greenPen = new Pen(Color.LightGreen, 5);

        // worker thread
        private Thread workerThread = null;

        Boolean needToStop = false, paused = false;
        QAgent qAgent;

        int interval = 30;

        string netFile = Environment.CurrentDirectory + "\\deepQnet.dat";
        
        public Form1()
        {
            InitializeComponent();

            // Fix Panel double buffering issue
            typeof(Panel).InvokeMember("DoubleBuffered",
            BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
            null, canvas, new object[] { true });
        }

        private void canvas_Paint(object sender, PaintEventArgs e)
        {
            if (qAgent != null) {
                displayBox.Text = qAgent.draw_world(e.Graphics);

                switch (qAgent.simspeed)
                {
                    case 0:
                        displayBox.Text += Environment.NewLine + "Simulation speed: Slow";
                        break;

                    case 1:
                        displayBox.Text += Environment.NewLine + "Simulation speed: Normal";
                        break;

                    case 2:
                        displayBox.Text += Environment.NewLine + "Simulation speed: Fast";
                        break;

                    case 3:
                        displayBox.Text += Environment.NewLine + "Simulation speed: Very Fast";
                        break;
                }
            }

            canvas.Update();
        }

        #region // Button Controls
        private void StopLearning_Click(object sender, EventArgs e)
        {
            qAgent.stoplearn();
        }

        private void startLearning_Click(object sender, EventArgs e)
        {
            if (qAgent == null)
            {
                var num_inputs = 27; // 9 eyes, each sees 3 numbers (wall, green, red thing proximity)
                var num_actions = 5; // 5 possible angles agent can turn
                var temporal_window = 4; // amount of temporal memory. 0 = agent lives in-the-moment :)
                var network_size = num_inputs * temporal_window + num_actions * temporal_window + num_inputs;

                var layer_defs = new List<LayerDefinition>();

                // the value function network computes a value of taking any of the possible actions
                // given an input state. Here we specify one explicitly the hard way
                // but user could also equivalently instead use opt.hidden_layer_sizes = [20,20]
                // to just insert simple relu hidden layers.
                layer_defs.Add(new LayerDefinition { type = "input", out_sx = 1, out_sy = 1, out_depth = network_size });
                layer_defs.Add(new LayerDefinition { type = "fc", num_neurons = 96, activation = "relu" });
                layer_defs.Add(new LayerDefinition { type = "fc", num_neurons = 96, activation = "relu" });
                layer_defs.Add(new LayerDefinition { type = "fc", num_neurons = 96, activation = "relu" });
                layer_defs.Add(new LayerDefinition { type = "regression", num_neurons = num_actions });

                // options for the Temporal Difference learner that trains the above net
                // by backpropping the temporal difference learning rule.
                //var opt = new Options { method="sgd", learning_rate=0.01, l2_decay=0.001, momentum=0.9, batch_size=10, l1_decay=0.001 };
                var opt = new Options { method = "adadelta", l2_decay = 0.001, batch_size = 10 };

                var tdtrainer_options = new TrainingOptions();
                tdtrainer_options.temporal_window = temporal_window;
                tdtrainer_options.experience_size = 30000;
                tdtrainer_options.start_learn_threshold = 1000;
                tdtrainer_options.gamma = 0.7;
                tdtrainer_options.learning_steps_total = 200000;
                tdtrainer_options.learning_steps_burnin = 3000;
                tdtrainer_options.epsilon_min = 0.05;
                tdtrainer_options.epsilon_test_time = 0.00;
                tdtrainer_options.layer_defs = layer_defs;
                tdtrainer_options.options = opt;

                var brain = new DeepQLearn(num_inputs, num_actions, tdtrainer_options);
                qAgent = new QAgent(brain, canvas.Width, canvas.Height);
            }
            else
                qAgent.startlearn();

            if (workerThread == null)
            {
                workerThread = new Thread(new ThreadStart(BackgroundThread));
                workerThread.Start();
            }
        }

        private void PauseBtn_Click(object sender, EventArgs e)
        {
            if(paused)
            {
                PauseBtn.Text = "Pause";
                paused = false;
            }
            else
            {
                PauseBtn.Text = "Continue";
                paused = true;
            }
        }

        private void saveNet_Click(object sender, EventArgs e)
        {
            // Save the netwok to file
            using (FileStream fstream = new FileStream(netFile, FileMode.Create))
            {
                new BinaryFormatter().Serialize(fstream, qAgent);
            }

            displayBox.Text = "QNetwork saved successfully";
        }

        private void loadNet_Click(object sender, EventArgs e)
        {
            // Load the netwok from file
            using (FileStream fstream = new FileStream(netFile, FileMode.Open))
            {
                qAgent = new BinaryFormatter().Deserialize(fstream) as QAgent;
                qAgent.Reinitialize();
            }

            if (workerThread == null)
            {
                workerThread = new Thread(new ThreadStart(BackgroundThread));
                workerThread.Start();
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            needToStop = true;

            if (workerThread != null)
            {
                // stop worker thread
                needToStop = true;
                while (!workerThread.Join(100))
                    Application.DoEvents();
                workerThread = null;
            }
        }

        private void goNormal_Click(object sender, EventArgs e)
        {
            qAgent.gonormal();
            interval = 25;
        }

        private void goFast_Click(object sender, EventArgs e)
        {
            qAgent.gofast();
            interval = 10;
        }

        private void goVeryFast_Click(object sender, EventArgs e)
        {
            qAgent.goveryfast();
            interval = 0;
        }

        private void goSlow_Click(object sender, EventArgs e)
        {
            qAgent.goslow();
            interval = 50;
        }
        #endregion

        // Delegates to enable async calls for setting controls properties
        private delegate void UpdateUICallback(Panel panel);

        // Thread safe updating of UI
        private void UpdateUI(Panel panel)
        {
            if(needToStop)
                return;

            if (panel.InvokeRequired)
            {
                UpdateUICallback d = new UpdateUICallback(UpdateUI);
                Invoke(d, new object[] { panel });
            }
            else
            {
                panel.Refresh();
            }
        }

        private void BackgroundThread()
        {
            while (!needToStop)
            {
                if (!paused)
                {
                    qAgent.tick();
                    UpdateUI(canvas);
                }

                Thread.Sleep(interval);
            }
        }

        public Intersect line_point_intersect(Graphics g, Vec A, Vec B, Vec C, double rad)
        {
            Intersect result = new Intersect { intersect = false };

            // compute the euclidean distance between A and B
            var LAB = Math.Sqrt(Math.Pow(B.x - A.x, 2) + Math.Pow(B.y - A.y, 2));

            // compute the direction vector of line AB
            var thetaAB = Math.Atan2((B.x - A.x), (B.y - A.y));

            // compute the direction vector D from A to B
            var Dx = (B.x - A.x) / LAB;
            var Dy = (B.y - A.y) / LAB;

            // Now the line equation is x = Dx*t + Ax, y = Dy*t + Ay with 0 <= t <= 1.
            // compute the value t of the closest point to the circle center (Cx, Cy)
            var t = Dx * (C.x - A.x) + Dy * (C.y - A.y);

            // This is the projection of C on the line from A to B.
            // compute the coordinates of the point E on line and closest to C
            var Ex = t * Dx + A.x;
            var Ey = t * Dy + A.y;

            // compute the euclidean distance from E to C
            var LEC = Math.Sqrt(Math.Pow(Ex - C.x, 2) + Math.Pow(Ey - C.y, 2));

            // test if the line intersects the circle
            if (LEC < rad)
            {
                // compute distance from t to circle intersection point
                var dt = Math.Sqrt(Math.Pow(rad, 2) - Math.Pow(LEC, 2));

                // compute first intersection point F
                var F = new Vec((t - dt) * Dx + A.x, (t - dt) * Dy + A.y);

                // compute second intersection point G
                var G = new Vec((t + dt) * Dx + A.x, (t + dt) * Dy + A.y);

                // compute the euclidean distance from A to F
                var LAF = Math.Sqrt(Math.Pow(F.x - A.x, 2) + Math.Pow(F.y - A.y, 2));

                // compute the direction vector of line AF
                var thetaAF = Math.Atan2((F.x - A.x),(F.y - A.y));

                if (LAF <= LAB && thetaAB == thetaAF)
                {
                    // line intersects with point
                    result = new Intersect { ua = dt, up = F, intersect = true };
                }

            }
            else if (LEC == rad) // else test if the line is tangent to circle
            {
                ;   // tangent point to circle is E
            }
            else 
            {
                ;   // line doesn't touch circle
            }

            return result;        
        }

        private void drawCircle(Graphics g, Vec center, int radius, Pen pen)
        {
            var rect = new Rectangle((int)center.x - radius, (int)center.y - radius, radius * 2, radius * 2);
            g.DrawEllipse(pen, rect);
        }

        private void drawLine(Graphics g, Vec a, Vec b, Pen pen)
        {
            Point[] points =
            {
                new Point((int)a.x, (int)a.y),
                new Point((int)b.x, (int)b.y)
            };

            g.DrawLines(pen, points);
        }
    }
}
