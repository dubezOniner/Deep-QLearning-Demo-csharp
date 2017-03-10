using ConvnetSharp;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace DeepQLearning.DRLAgent
{
    [Serializable]
    public struct Intersect
    {
        public double ua;
        public double ub;
        public Vec up;
        public int type;
        public bool intersect;
    };

    // A 2D vector utility
    [Serializable]
    public class Vec
    {
        public double x, y;

        public Vec (double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        // utilities
        public double dist_from(Vec v) { return Math.Sqrt(Math.Pow(this.x - v.x, 2) + Math.Pow(this.y - v.y, 2)); }
        public double length() { return Math.Sqrt(Math.Pow(this.x, 2) + Math.Pow(this.y, 2)); }

        // new vector returning operations
        public Vec add(Vec v) { return new Vec(this.x + v.x, this.y + v.y); }
        public Vec sub(Vec v) { return new Vec(this.x - v.x, this.y - v.y); }
        public Vec rotate(double a)
        {  // CLOCKWISE
            return new Vec(this.x * Math.Cos(a) + this.y * Math.Sin(a),
                           -this.x * Math.Sin(a) + this.y * Math.Cos(a));
        }
      
        // in place operations
        public void scale(double s) { this.x *= s; this.y *= s; }
        public void normalize() { var d = this.length(); this.scale(1.0 / d); }
    }

    // Wall is made up of two points
    [Serializable]
    public class Wall
    {
        public Vec p1, p2;

        public Wall(Vec p1, Vec p2)
        {
            this.p1 = p1;
            this.p2 = p2;
        }
    }

    // Eye sensor has a maximum range and senses walls
    [Serializable]
    public class Eye
    {
        public double angle;
        public double max_range;
        public double sensed_proximity;
        public int sensed_type;

        public Eye(double angle)
        {
            this.angle = angle; // angle of the eye relative to the agent
            this.max_range = 85; // maximum proximity range
            this.sensed_proximity = 85; // proximity of what the eye is seeing. will be set in world.tick()
            this.sensed_type = -1; // what type of object does the eye see?
        }
    }

    // item is circle thing on the floor that agent can interact with (see or eat, etc)
    [Serializable]
    public class Item
    {
        public Vec p;
        public int type;
        public double rad;
        public int age;
        public bool cleanup_;

        public Item(double x, double y, int type)
        {
            this.p = new Vec(x, y); // position
            this.type = type;
            this.rad = 10; // default radius
            this.age = 0;
            this.cleanup_ = false;
        }
    }

    // A single agent
    [Serializable]
    public class Agent
    {
        public List<Eye> eyes;
        public List<double[]> actions;
        public double angle, oangle, reward_bonus, digestion_signal;
        public double rad, rot1, rot2, prevactionix;
        public Vec p, op;
        public int actionix;
        public DeepQLearn brain;

        public Agent(DeepQLearn brain)
        {
            this.brain = brain;

            // positional information
            this.p = new Vec(50, 50);
            this.op = this.p; // old position
            this.angle = 0; // direction facing

            this.actions = new List<double[]>();
            this.actions.Add(new double[] { 1, 1 });
            this.actions.Add(new double[] { 0.8, 1 });
            this.actions.Add(new double[] { 1, 0.8 });
            this.actions.Add(new double[] { 0.5, 0 });
            this.actions.Add(new double[] { 0, 0.5 });

            // properties
            this.rad = 10;
            this.eyes = new List<Eye>();
            for (var k = 0; k < 9; k++) { this.eyes.Add(new Eye((k - 3) * 0.25)); }

            this.reward_bonus = 0.0;
            this.digestion_signal = 0.0;

            // outputs on world
            this.rot1 = 0.0; // rotation speed of 1st wheel
            this.rot2 = 0.0; // rotation speed of 2nd wheel

            this.prevactionix = -1;
        }

        public void forward()
        {
            // in forward pass the agent simply behaves in the environment
            // create input to brain
            var num_eyes = this.eyes.Count;
            var input_array = new double[num_eyes * 3];
            for (var i = 0; i < num_eyes; i++)
            {
                var e = this.eyes[i];
                input_array[i * 3] = 1.0;
                input_array[i * 3 + 1] = 1.0;
                input_array[i * 3 + 2] = 1.0;
                if (e.sensed_type != -1)
                {
                    // sensed_type is 0 for wall, 1 for food and 2 for poison.
                    // lets do a 1-of-k encoding into the input array
                    input_array[i * 3 + e.sensed_type] = e.sensed_proximity / e.max_range; // normalize to [0,1]
                }
            }

            Volume input = new Volume(num_eyes, 3, 1);
            input.w = input_array;

            // get action from brain
            var actionix = this.brain.forward(input);
            var action = this.actions[actionix];
            this.actionix = actionix; //back this up

            // demultiplex into behavior variables
            this.rot1 = action[0] * 1;
            this.rot2 = action[1] * 1;

            //this.rot1 = 0;
            //this.rot2 = 0;
        }

        public void backward()
        {
            // in backward pass agent learns.
            // compute reward 
            var proximity_reward = 0.0;
            var num_eyes = this.eyes.Count;
            for (var i = 0; i < num_eyes; i++)
            {
                var e = this.eyes[i];
                // agents dont like to see walls, especially up close
                proximity_reward += e.sensed_type == 0 ? e.sensed_proximity / e.max_range : 1.0;
            }
            proximity_reward = proximity_reward / num_eyes;
            proximity_reward = Math.Min(1.0, proximity_reward * 2);

            // agents like to go straight forward
            var forward_reward = 0.0;
            if (this.actionix == 0 && proximity_reward > 0.75) forward_reward = 0.1 * proximity_reward;

            // agents like to eat good things
            var digestion_reward = this.digestion_signal;
            this.digestion_signal = 0.0;

            var reward = proximity_reward + forward_reward + digestion_reward;

            // pass to brain for learning
            this.brain.backward(reward);
        }
    }

    // World object contains many agents and walls and food and stuff
    [Serializable]
    public class World
    {
        Util util;

        int W, H;
        int clock;

        public List<Wall> walls;
        public List<Item> items;
        public List<Agent> agents;

        List<Intersect> collpoints;

        public World(DeepQLearn brain, int canvas_Width, int canvas_Height)
        {
            this.agents = new List<Agent>();
            this.W = canvas_Width;
            this.H = canvas_Height;

            this.util = new Util();
            this.clock = 0;

            // set up walls in the world
            this.walls = new List<Wall>();
            var pad = 10;

            util_add_box(this.walls, pad, pad, this.W - pad * 2, this.H - pad * 2);
            util_add_box(this.walls, 100, 100, 200, 300); // inner walls

            this.walls.RemoveAt(walls.Count - 1);
            util_add_box(this.walls, 400, 100, 200, 300);
            this.walls.RemoveAt(walls.Count - 1);

            // set up food and poison
            this.items = new List<Item>();
            for (var k = 0; k < 30; k++)
            {
                var x = util.randf(20, this.W - 20);
                var y = util.randf(20, this.H - 20);
                var t = util.randi(1, 3); // food or poison (1 and 2)
                var it = new Item(x, y, t);
                this.items.Add(it);
            }

            // set up food and poison
            this.agents = new List<Agent>();
            this.agents.Add(new Agent(brain));
        }

        private void util_add_box(List<Wall> lst, double x, double y, double w, double h)
        {
            lst.Add(new Wall(new Vec(x, y), new Vec(x + w, y)));
            lst.Add(new Wall(new Vec(x + w, y), new Vec(x + w, y + h)));
            lst.Add(new Wall(new Vec(x + w, y + h), new Vec(x, y + h)));
            lst.Add(new Wall(new Vec(x, y + h), new Vec(x, y)));
        }

        // helper function to get closest colliding walls/items
        public Intersect stuff_collide_(Vec p1, Vec p2, bool check_walls, bool check_items)
        {
            Intersect minres = new Intersect() { intersect = false };

            // collide with walls
            if (check_walls)
            {
                for (int i = 0, n = this.walls.Count; i < n; i++)
                {
                    var wall = this.walls[i];
                    var res = line_intersect(p1, p2, wall.p1, wall.p2);
                    if (res.intersect)
                    {
                        res.type = 0; // 0 is wall
                        if (!minres.intersect)
                        {
                            minres = res;
                        }
                        else
                        {   // check if its closer
                            if (res.ua < minres.ua)
                            {
                                // if yes replace it
                                minres = res;
                            }
                        }
                    }
                }
            }

            // collide with items
            if (check_items)
            {
                for (int i = 0, n = this.items.Count; i < n; i++)
                {
                    var it = this.items[i];
                    var res = line_point_intersect(p1, p2, it.p, it.rad);
                    if (res.intersect)
                    {
                        res.type = it.type; // store type of item
                        if (!minres.intersect) { minres = res; }
                        else
                        {   // check if its closer
                            if (res.ua < minres.ua)
                            {
                                // if yes replace it
                                minres = res;
                            }
                        }
                    }
                }
            }

            return minres;
        }

        // line intersection helper function: does line segment (p1,p2) intersect segment (p3,p4) ?
        public Intersect line_intersect(Vec p1, Vec p2, Vec p3, Vec p4)
        {
            Intersect result = new Intersect() { intersect= false };

            var denom = (p4.y - p3.y) * (p2.x - p1.x) - (p4.x - p3.x) * (p2.y - p1.y);
            if (denom == 0.0) { result.intersect = false; } // parallel lines

            var ua = ((p4.x - p3.x) * (p1.y - p3.y) - (p4.y - p3.y) * (p1.x - p3.x)) / denom;
            var ub = ((p2.x - p1.x) * (p1.y - p3.y) - (p2.y - p1.y) * (p1.x - p3.x)) / denom;
            if (ua > 0.0 && ua < 1.0 && ub > 0.0 && ub < 1.0)
            {
                var up = new Vec(p1.x + ua * (p2.x - p1.x), p1.y + ua * (p2.y - p1.y));
                return new Intersect { ua = ua, ub = ub, up = up, intersect = true }; // up is intersection point
            }
            return result;
        }
        
        public Intersect  line_point_intersect(Vec A, Vec B, Vec C, double rad) {

            Intersect result = new Intersect { intersect = false };

            var v = new Vec(B.y-A.y,-(B.x-A.x)); // perpendicular vector
            var d = Math.Abs((B.x-A.x)*(A.y-C.y)-(A.x-C.x)*(B.y-A.y));
            d = d / v.length();
            if(d > rad) { return result; }
      
            v.normalize();
            v.scale(d);
            double ua = 0.0;
            var up = C.add(v);
            if(Math.Abs(B.x-A.x)>Math.Abs(B.y-A.y)) {
                ua = (up.x - A.x) / (B.x - A.x);
            } else {
                ua = (up.y - A.y) / (B.y - A.y);
            }
            if(ua>0.0 && ua<1.0) {
                result = new Intersect { ua = ua, up = up, intersect = true };
            }
            return result;
        }

        private Boolean AreSimilar(double a, double b, double tolerance)
        {
            // Values are within specified tolerance of each other....
            return Math.Abs(a - b) < tolerance;
        }

        public void tick()
        {
            // tick the environment
            this.clock++;

            // fix input to all agents based on environment process eyes
            this.collpoints = new List<Intersect>();
            for (int i = 0, n = this.agents.Count; i < n; i++)
            {
                var a = this.agents[i];
                for (int ei = 0, ne = a.eyes.Count; ei < ne; ei++)
                {
                    var e = a.eyes[ei];
                    // we have a line from p to p->eyep
                    var eyep = new Vec(a.p.x + e.max_range * Math.Sin(a.angle + e.angle), a.p.y + e.max_range * Math.Cos(a.angle + e.angle));
                    var res = this.stuff_collide_(a.p, eyep, true, true);

                    if (res.intersect)
                    {
                        // eye collided with wall
                        e.sensed_proximity = res.up.dist_from(a.p);
                        e.sensed_type = res.type;
                    }
                    else
                    {
                        e.sensed_proximity = e.max_range;
                        e.sensed_type = -1;
                    }
                }
            }

            // let the agents behave in the world based on their input
            for (int i = 0, n = this.agents.Count; i < n; i++)
            {
                this.agents[i].forward();
            }

            // apply outputs of agents on evironment
            for (int i = 0, n = this.agents.Count; i < n; i++)
            {
                var a = this.agents[i];
                a.op = a.p; // back up old position
                a.oangle = a.angle; // and angle

                // steer the agent according to outputs of wheel velocities
                var v = new Vec(0, a.rad / 2.0);
                v = v.rotate(a.angle + Math.PI / 2);
                var w1p = a.p.add(v); // positions of wheel 1 and 2
                var w2p = a.p.sub(v);
                var vv = a.p.sub(w2p);
                vv = vv.rotate(-a.rot1);
                var vv2 = a.p.sub(w1p);
                vv2 = vv2.rotate(a.rot2);
                var np = w2p.add(vv);
                np.scale(0.5);
                var np2 = w1p.add(vv2);
                np2.scale(0.5);
                a.p = np.add(np2);

                a.angle -= a.rot1;
                if (a.angle < 0) a.angle += 2 * Math.PI;
                a.angle += a.rot2;
                if (a.angle > 2 * Math.PI) a.angle -= 2 * Math.PI;

                // agent is trying to move from p to op. Check walls
                var res = this.stuff_collide_(a.op, a.p, true, false);
                if (res.intersect)
                {
                    // wall collision! reset position
                    a.p = a.op;
                }

                // handle boundary conditions
                if (a.p.x < 0) a.p.x = 0;
                if (a.p.x > this.W) a.p.x = this.W;
                if (a.p.y < 0) a.p.y = 0;
                if (a.p.y > this.H) a.p.y = this.H;
            }

            // tick all items
            var update_items = false;
            for (int i = 0, n = this.items.Count; i < n; i++)
            {
                var it = this.items[i];
                it.age += 1;

                // see if some agent gets lunch
                for (int j = 0, m = this.agents.Count; j < m; j++)
                {
                    var a = this.agents[j];
                    var d = a.p.dist_from(it.p);
                    if (d < it.rad + a.rad)
                    {

                        // wait lets just make sure that this isn't through a wall
                        var rescheck = this.stuff_collide_(a.p, it.p, true, false);
                        if (!rescheck.intersect)
                        {
                            // ding! nom nom nom
                            if (it.type == 1) a.digestion_signal += 5.0; // mmm delicious apple
                            if (it.type == 2) a.digestion_signal += -6.0; // ewww poison
                            it.cleanup_ = true;
                            update_items = true;
                            break; // break out of loop, item was consumed
                        }
                    }
                }

                if (it.age > 5000 && this.clock % 100 == 0 && util.randf(0, 1) < 0.1)
                {
                    it.cleanup_ = true; // replace this one, has been around too long
                    update_items = true;
                }
            }
            if (update_items)
            {
                var nt = new List<Item>();
                for (int i = 0, n = this.items.Count; i < n; i++)
                {
                    var it = this.items[i];
                    if (!it.cleanup_) nt.Add(it);
                }
                this.items = nt; // swap
            }
            if (this.items.Count < 30 && this.clock % 10 == 0 && util.randf(0, 1) < 0.25)
            {
                var newitx = util.randf(20, this.W - 20);
                var newity = util.randf(20, this.H - 20);
                var newitt = util.randi(1, 3); // food or poison (1 and 2)
                var newit = new Item(newitx, newity, newitt);
                this.items.Add(newit);
            }

            // agents are given the opportunity to learn based on feedback of their action on environment
            for (int i = 0, n = this.agents.Count; i < n; i++)
            {
                this.agents[i].backward();
            }
        }
    }

    [Serializable]
    public class QAgent
    {
        public int simspeed = 1;
        World w;

        [NonSerialized]
        Pen greenPen = new Pen(Color.LightGreen, 2);

        [NonSerialized]
        Pen redPen = new Pen(Color.Red, 2);

        [NonSerialized]
        Pen greenPen2 = new Pen(Color.LightGreen, 1);

        [NonSerialized]
        Pen redPen2 = new Pen(Color.Red, 1);

        [NonSerialized]
        Pen bluePen = new Pen(Color.Blue, 2);

        [NonSerialized]
        Pen blackPen = new Pen(Color.Black);
        
        public QAgent(DeepQLearn brain, int canvas_W, int canvas_H)
        {
            this.w = new World(brain, canvas_W, canvas_H);
        }

        public void Reinitialize()
        {
            greenPen = new Pen(Color.LightGreen, 2);
            redPen = new Pen(Color.Red, 2);
            greenPen2 = new Pen(Color.LightGreen, 1);
            redPen2 = new Pen(Color.Red, 1);
            bluePen = new Pen(Color.Blue, 2);
            blackPen = new Pen(Color.Black);

            this.simspeed = 1;
            this.w.agents[0].brain.learning = false;
            this.w.agents[0].brain.epsilon_test_time = 0.01;

            this.w.agents[0].op.x = 500;
            this.w.agents[0].op.y = 500;
        }

        public void tick()
        {
            w.tick();
        }

        // Draw everything and return stats
        public string draw_world(Graphics g)
        {
            var agents = w.agents;

            // draw walls in environment
            for (int i = 0, n = w.walls.Count; i < n; i++)
            {
                var q = w.walls[i];
                drawLine(g, q.p1, q.p2, blackPen);
            }

            // draw agents
            for (int i = 0, n = agents.Count; i < n; i++)
            {
                // draw agent's body
                var a = agents[i];
                drawArc(g, a.op, (int)a.rad, 0, (float)(Math.PI * 2), blackPen);

                // draw agent's sight
                for (int ei = 0, ne = a.eyes.Count; ei < ne; ei++)
                {
                    var e = a.eyes[ei];
                    var sr = e.sensed_proximity;
                    Pen pen;

                    if (e.sensed_type == 1) pen = redPen2;           // apples
                    else if (e.sensed_type == 2) pen = greenPen2;    // poison
                    else pen = blackPen;                            // wall

                    //var new_x = a.op.x + sr * Math.Sin(radToDegree((float)a.oangle) + radToDegree((float)e.angle));
                    //var new_y = a.op.y + sr * Math.Cos(radToDegree((float)a.oangle) + radToDegree((float)e.angle));

                    var new_x = a.op.x + sr * Math.Sin(a.oangle + e.angle);
                    var new_y = a.op.y + sr * Math.Cos(a.oangle + e.angle);
                    Vec b = new Vec(new_x, new_y);

                    drawLine(g, a.op, b, pen);
                }
            }

            // draw items
            for (int i = 0, n = w.items.Count; i < n; i++)
            {
                Pen pen = blackPen;
                var it = w.items[i];
                if (it.type == 1) pen = redPen; 
                if (it.type == 2) pen = greenPen;

                drawArc(g, it.p, (int)it.rad, 0, (float)(Math.PI * 2), pen);
            }

            return w.agents[0].brain.visSelf();
        }

        public void goveryfast()
        {
            simspeed = 3;
        }

        public void gofast()
        {
            simspeed = 2;
        }

        public void gonormal()
        {
            simspeed = 1;
        }

        public void goslow()
        {
            simspeed = 0;
        }

        public void startlearn()
        {
            this.w.agents[0].brain.learning = true;
        }

        public void stoplearn()
        {
            this.w.agents[0].brain.learning = false;            
        }

        private void drawCircle(Graphics g, Vec center, int radius, Pen pen)
        {
            var rect = new Rectangle((int)center.x - radius, (int)center.y - radius, radius * 2, radius * 2);
            g.DrawEllipse(pen, rect);
        }

        private void drawArc(Graphics g, Vec center, int radius, float startAngle, float sweepAngle, Pen pen)
        {
            var rect = new Rectangle((int)center.x - radius, (int)center.y - radius, radius * 2, radius * 2);
            g.DrawArc(pen, rect, radToDegree(startAngle), radToDegree(sweepAngle));
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

        private float radToDegree(float rad)
        {
            return (float)(rad * 180 / Math.PI);
        }
    }
}
