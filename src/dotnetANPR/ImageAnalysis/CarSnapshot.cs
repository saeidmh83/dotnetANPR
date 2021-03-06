﻿using System.Collections.Generic;
using System.Drawing;
using dotnetANPR.ImageAnalysis.Convolution;

namespace dotnetANPR.ImageAnalysis
{
    public class CarSnapshot : Photo
    {
        private static readonly int distributorMargins =
            Intelligence.Intelligence.Configurator.GetIntProperty("carsnapshot_distributormargins");

        private static readonly int carSnapshotGraphRankFilter =
            Intelligence.Intelligence.Configurator.GetIntProperty("carsnapshot_graphrankfilter");

        private static readonly int numberOfCandidates =
            Intelligence.Intelligence.Configurator.GetIntProperty("intelligence_numberOfBands");

        public static Graph.ProbabilityDistributor Distributor = new Graph.ProbabilityDistributor(0, 0, distributorMargins, distributorMargins);

        private CarSnapshotGraph graphHandle;

        public CarSnapshot() {}

        public CarSnapshot(string filePath) : base(filePath) {}

        public CarSnapshot(Bitmap bitmap) : base(bitmap) {}

        public Bitmap RenderGraph()
        {
            ComputeGraph();
            return graphHandle.RenderVertically(100, GetHeight());
        }

        private List<Graph.Peak> ComputeGraph()
        {
            if (graphHandle != null)
            {
                return graphHandle.Peaks;
            }
            var bitmap = DuplicateBitmap(Image);
            VerticalEdgeBitmap(bitmap);
            Thresholding(bitmap);

            graphHandle = Histogram(bitmap);
            graphHandle.RankFilter(carSnapshotGraphRankFilter);
            graphHandle.ApplyProbabilityDistributor(Distributor);
            graphHandle.FindPeaks(numberOfCandidates);
            return graphHandle.Peaks;
        }

        public List<Band> GetBands()
        {
            var output = new List<Band>();
            var peaks = ComputeGraph();
            for (var i = 0; i < peaks.Count; i++)
            {
                var peak = peaks[i];
                output.Add(new Band(
                        Image.Clone(new Rectangle(
                                0,
                                peak.Left,
                                Image.Width,
                                peak.GetDiff()
                            ),
                            Image.PixelFormat
                        )
                    )
                );
            }
            return output;
        }

        public void VerticalEdgeBitmap(Bitmap image)
        {
            var imageCopy = DuplicateBitmap(image);
            int[,] data =
            {
                {-1, 0, 1},
                {-1, 0, 1},
                {-1, 0, 1},
                {-1, 0, 1}
            };
            var convolveOp = new ConvolveOp();
            var kernel = new ConvolutionKernel();
            kernel.Size = 3;
            kernel.Matrix = data;
            imageCopy = convolveOp.Convolve(image, kernel);
        }

        public CarSnapshotGraph Histogram(Bitmap bitmap)
        {
            var graph = new CarSnapshotGraph(this);
            for (var y = 0; y < bitmap.Height; y++)
            {
                float counter = 0;
                for (var x = 0; x < bitmap.Width; x++)
                    counter += GetBrightness(bitmap, x, y);
                graph.AddPeak(counter);
            }
            return graph;
        }
    }
}
