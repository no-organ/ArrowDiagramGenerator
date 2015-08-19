﻿using GraphVizWrapper;
using GraphVizWrapper.Commands;
using GraphVizWrapper.Queries;
using QuickGraph;
using QuickGraph.Algorithms;
using QuickGraph.Graphviz;
using QuickGraph.Serialization;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace TransitiveReduction
{
    class Program
    {
        static Form f;
        static BidirectionalGraph<int, SEdge<int>> graph;
        static BidirectionalGraph<ADVertex, ADEdge<ADVertex>> adGraph;

        static void Main(string[] args)
        {
            

            var edges = new SEdge<int>[] {
                new SEdge<int>(1 ,2),
                new SEdge<int>(2 ,3),
                new SEdge<int>(3 ,8),
                new SEdge<int>(3 ,6),
                new SEdge<int>(3 ,10),
                new SEdge<int>(3 ,7),
                new SEdge<int>(3 ,9),
                new SEdge<int>(6 ,12),
                new SEdge<int>(10 ,12),
                new SEdge<int>(3 ,4),
                new SEdge<int>(6 ,11),
                new SEdge<int>(9 ,11),
                new SEdge<int>(6 ,13),
                new SEdge<int>(6 ,16),
                new SEdge<int>(12 ,14),
                new SEdge<int>(13 ,14),
                new SEdge<int>(12 ,15),
                new SEdge<int>(13 ,15),

                new SEdge<int>(4 ,5),
                
                new SEdge<int>(7 ,17),
                new SEdge<int>(8 ,17),
                new SEdge<int>(11 ,17),
                new SEdge<int>(14 ,17),
                new SEdge<int>(15 ,17),

                new SEdge<int>(7 ,18),
                new SEdge<int>(8 ,18),
                new SEdge<int>(15 ,18),
                new SEdge<int>(16 ,18),

                new SEdge<int>(17 ,19),
                new SEdge<int>(18 ,19),

                new SEdge<int>(17 ,20),
                
                new SEdge<int>(5 ,21),
                new SEdge<int>(19 ,21),
                new SEdge<int>(20 ,21)
            };

            graph = edges.ToBidirectionalGraph<int, SEdge<int>>();

            /*
            var graph = new BidirectionalGraph<int, SEdge<int>>();
            var deserializer = new GraphMLDeserializer<int, SEdge<int>, BidirectionalGraph<int, SEdge<int>>>();

            using (var xreader = XmlReader.Create("in.graphml"))
            {
                deserializer.Deserialize(xreader, graph, (string i) => Int32.Parse(i), (int s, int t, string id) => new SEdge<int>(s, t));
            }
            */

            f = new Form();
            f.Width = 700;
            f.Height = 1024;
            
            PictureBox pb = new PictureBox(){ Dock = DockStyle.Fill};
            pb.SizeMode = PictureBoxSizeMode.StretchImage;
            
            pb.Top = 0;
            pb.Left = 0;

            f.Controls.Add(pb);
            f.Show();

            f.FormClosed += f_FormClosed;
            pb.Click += pb_Click;

            Application.Run();
        }

        static int step = 0;
        static void pb_Click(object sender, EventArgs e)
        {
            bool shouldExit = false;
            MemoryStream memstream = null;

            if (step == 0)
            {
                var outFileData = OutputToDotFile(graph);
                memstream = new MemoryStream(outFileData);

                var serialzier = new GraphMLSerializer<int, SEdge<int>, BidirectionalGraph<int, SEdge<int>>>();
                using (var xwriter = XmlWriter.Create("out.graphml"))
                {
                    serialzier.Serialize(xwriter, graph, (int v) => v.ToString(), (SEdge<int> edge) => String.Format("{0}-{1}", edge.Source.ToString(), edge.Target.ToString()));
                }
            }
            else if (step == 1)
            {
                var algo = new TransitiveClosureAlgorithm(graph);
                algo.Compute();
                graph = algo.TransitiveClosure;

                //var serialzier = new GraphMLSerializer<int, SEdge<int>, BidirectionalGraph<int, SEdge<int>>>();
                //using (var xwriter = XmlWriter.Create("out.graphml"))
                //{
                //serialzier.Serialize(xwriter, transitiveClosure, (int v) => v.ToString(), (SEdge<int> edge) => String.Format("{0}-{1}", edge.Source.ToString(), edge.Target.ToString()));
                //}

                var outFileData = OutputToDotFile(graph);
                memstream = new MemoryStream(outFileData);
            }
            else if (step == 2)
            {
                adGraph = GenerateADGraph(graph);

                var outFileData = OutputToDotFile(adGraph);
                memstream = new MemoryStream(outFileData);
                // 
            }
            else if (step == 3)
            {
                ReduceADGraph(adGraph);

                var outFileData = OutputToDotFile(adGraph);
                memstream = new MemoryStream(outFileData);
            }
            else
            {
                shouldExit = true;
            }

            if (memstream != null)
            {
                var pb = f.Controls[0] as PictureBox;
                pb.Image = new Bitmap(memstream);
            }
            step++;


            if (shouldExit) f.Close();
        }

        static void f_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        public static byte[] OutputToDotFile(BidirectionalGraph<int, SEdge<int>> graph)
        {
            byte[] result = null;
            var getStartProcessQuery = new GetStartProcessQuery();
            var getProcessStartInfoQuery = new GetProcessStartInfoQuery();
            var registerLayoutPluginCommand = new RegisterLayoutPluginCommand(getProcessStartInfoQuery, getStartProcessQuery);
            var wrapper = new GraphGeneration(getStartProcessQuery, getProcessStartInfoQuery, registerLayoutPluginCommand);

            StringBuilder sb = new StringBuilder();

            sb.Append("digraph G {\n");
            foreach (var vertex in graph.Vertices)
            {
                sb.AppendFormat("{0} ;\n", vertex);
            }

            foreach (var edge in graph.Edges)
            {
                sb.AppendFormat("{0} -> {1} ;\n", edge.Source, edge.Target);
            }
            sb.Append("}");

            using (var fwriter = File.Create("out.dot"))
            {
                Byte[] info = new UTF8Encoding(true).GetBytes(sb.ToString());
                fwriter.Write(info, 0, info.Length);  
            }

            result = wrapper.GenerateGraph(sb.ToString(), Enums.GraphReturnType.Png);

            return result;
        }

        public static byte[] OutputToDotFile(BidirectionalGraph<ADVertex, ADEdge<ADVertex>> graph)
        {
            byte[] result = null;
            var getStartProcessQuery = new GetStartProcessQuery();
            var getProcessStartInfoQuery = new GetProcessStartInfoQuery();
            var registerLayoutPluginCommand = new RegisterLayoutPluginCommand(getProcessStartInfoQuery, getStartProcessQuery);
            var wrapper = new GraphGeneration(getStartProcessQuery, getProcessStartInfoQuery, registerLayoutPluginCommand);

            StringBuilder sb = new StringBuilder();

            sb.Append("digraph G {\n");
            foreach (var vertex in graph.Vertices)
            {
                sb.AppendFormat("{0} [ id={1} ];\n", vertex, vertex.ToString());
            }

            foreach (var edge in graph.Edges)
            {
                if (edge.ActivityId.HasValue)
                {
                    sb.AppendFormat("{0} -> {1} [ id={2} label={2} ];\n", edge.Source, edge.Target, edge.ActivityId);
                }
                else
                {
                    sb.AppendFormat("{0} -> {1} [ style=dashed ];\n", edge.Source, edge.Target);
                }
            }
            sb.Append("}");

            using (var fwriter = File.Create("out.dot"))
            {
                Byte[] info = new UTF8Encoding(true).GetBytes(sb.ToString());
                fwriter.Write(info, 0, info.Length);
            }

            result = wrapper.GenerateGraph(sb.ToString(), Enums.GraphReturnType.Png);

            return result;
        }

        public static BidirectionalGraph<ADVertex, ADEdge<ADVertex>> GenerateADGraph(BidirectionalGraph<int, SEdge<int>> nodeGraph)
        {
            var adGraph = new BidirectionalGraph<ADVertex, ADEdge<ADVertex>>();

            // Go over all vertice - add them as a new activity edges.
            // activity vertex name are important for resuse when adding the edges.
            foreach (var vertex in nodeGraph.Vertices)
            {
                var startNode = ADVertex.New(vertex, ActivityVertexType.ActivityStart);
                var endNode = ADVertex.New(vertex, ActivityVertexType.ActivityEnd);
                adGraph.AddVertex(startNode);
                adGraph.AddVertex(endNode);

                ADEdge<ADVertex> activityEdge = new ADEdge<ADVertex>(startNode, endNode, vertex);

                adGraph.AddEdge(activityEdge);
            }

            // Go over all edges - convert them to activity edges.
            // Make sure connections are maintained.
            foreach (var edge in nodeGraph.Edges)
            {
                ADEdge<ADVertex> activityEdge = new ADEdge<ADVertex>(
                    ADVertex.New(edge.Source, ActivityVertexType.ActivityEnd),
                    ADVertex.New(edge.Target, ActivityVertexType.ActivityStart));

                adGraph.AddEdge(activityEdge);
            }
            
            return adGraph;
        }

        private static void ReduceADGraph(BidirectionalGraph<ADVertex, ADEdge<ADVertex>> adGraph)
        {
            // Go over every vertex
            foreach (var vertex in adGraph.Vertices)
            {
                // We only care at the moment about activity end vertice
                if (vertex.Type == ActivityVertexType.ActivityEnd)
                {
                    // Get all the edges going out of this vertex
                    IEnumerable<ADEdge<ADVertex>> foundOutEdges;
                    if (adGraph.TryGetOutEdges(vertex, out foundOutEdges))
                    {
                        var commonDependenciesForAllTargets = new HashSet<ADVertex>();
                        // Find the common dependencies for all target vertice
                        foreach (var outEdge in foundOutEdges)
                        {
                            var target = outEdge.Target;
                            if (target.Type == ActivityVertexType.ActivityStart)
                            {
                                IEnumerable<ADEdge<ADVertex>> dependenciesOfTarget;
                                if (adGraph.TryGetInEdges(target, out dependenciesOfTarget))
                                {
                                    if (commonDependenciesForAllTargets.Count == 0)
                                    {
                                        foreach (var dependency in dependenciesOfTarget)
                                        {
                                            commonDependenciesForAllTargets.Add(dependency.Source);
                                        }
                                    }
                                    else
                                    {
                                        commonDependenciesForAllTargets.IntersectWith(dependenciesOfTarget.Select(d => d.Source).AsEnumerable());
                                    }
                                }
                                // Else can never happen - the out edge for the current vertice is the in edge of the dependent
                                // so at least once exists.
                            }
                            else // That means the inspected vertice has a dependent which is not an activity (need to inspect these cases)
                            {
                            }
                        }

                        // Now, if we have some common dependncies of all targets which are not the current vertex - they should be redirected
                        foreach (var commonDependency in commonDependenciesForAllTargets.Where(d => d != vertex))
                        {
                            IEnumerable<ADEdge<ADVertex>> edgesOutOfDependency;
                            if (adGraph.TryGetOutEdges(commonDependency, out edgesOutOfDependency))
                            {
                                var depndents = foundOutEdges.Select(e => e.Target);

                                // This dependency should no longer point at the dependents of this vertex
                                var edgesToRemove = edgesOutOfDependency.Where(e => depndents.Contains(e.Target)).ToList();
                                
                                foreach (var edgeToRemove in edgesToRemove)
                                {
                                    adGraph.RemoveEdge(edgeToRemove);
                                }
                            }
                            // Else should never happen

                            // This dependency should point at this vertex
                            var edgeToAdd = new ADEdge<ADVertex>(commonDependency, vertex);
                            adGraph.AddEdge(edgeToAdd);
                        }
                    }
                }
            }
        }
    }
}
