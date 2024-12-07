using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IsoVistGH {
    internal static class Geometry {
        internal static readonly double Tolerance = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
        
        /// <summary>
        /// Create the cross product from two vectors.
        /// </summary>
        /// <param name="vec1">
        /// The first vector.
        /// </param>
        /// <param name="vec2">
        /// The second vector.
        /// </param>
        /// <returns>
        /// The cross product of two vectors.
        /// </returns>
        private static Vector3d VecCrossProd(Vector3d vec1, Vector3d vec2) {
            return new Vector3d(vec1.Y * vec2.Z - vec1.Z * vec2.Y, vec1.Z * vec2.X - vec1.X * vec2.Z, vec1.X * vec2.Y - vec1.Y * vec2.X);
        }
        /// <summary>
        /// Check if two vectors are parallel to each other.
        /// </summary>
        /// <param name="vecA">
        /// The first vector.
        /// </param>
        /// <param name="vecB">
        /// The second vector.
        /// </param>
        /// <returns>
        /// True if the vectors are parallel, else False.
        /// </returns>
        private static bool IsParallel(Vector3d vecA, Vector3d vecB) {
            int parallelCheck = vecA.IsParallelTo(vecB, RhinoMath.DefaultAngleTolerance);
            if (parallelCheck == -1 || parallelCheck == 1) {
                return true;
            }
            return false;
        }
        /// <summary>
        /// Check if a point is on a plane.
        /// </summary>
        /// <param name="pt">
        /// The point to be checked.
        /// </param>
        /// <param name="plane">
        /// The plance that is used as a reference.
        /// </param>
        /// <returns>
        /// True if poiknt is on the provided plane, else False.
        /// </returns>
        private static bool IsPtOnPlane(this Point3d pt, Plane plane) {
            double distance = plane.DistanceTo(pt);
            if (Math.Abs(distance) > Tolerance) {
                return false;
            }
            return true;
        }
        /// <summary>
        /// Create the dot product form two vectors.
        /// </summary>
        /// <param name="vec1">
        /// The first vector.
        /// </param>
        /// <param name="vec2">
        /// The second vector.
        /// </param>
        /// <returns>
        /// The dot product of two vectors.
        /// </returns>
        private static double VecDotProd(Vector3d vec1, Vector3d vec2) {
            return (vec1.X * vec2.X + vec1.Y * vec2.Y + vec1.Z * vec2.Z);
        }
        /// <summary>
        /// Check if a set of (closed) regions all lie on the same plane.
        /// </summary>
        /// <param name="regions">
        /// The set of (closed) regions to be checked.
        /// </param>
        /// <returns>
        /// True if regions lie on the same plane, else False.
        /// </returns>
        private static bool AreCoplanar(this Curve[] regions) {
            foreach (Curve region in regions) {
                if (!region.IsPlanar()) { return false; }
            }

            Plane plane = regions[0].PlaneFromRegion();
            for (int i = 1; i < regions.Length; i++) {
                if (!regions[i].PointAtStart.IsPtOnPlane(plane)) { return false; }
            }
            return true;

        }
        /// <summary>
        /// Check if two (closed) regions lie on the same plane.
        /// </summary>
        /// <param name="region1">
        /// The firs plane to check.
        /// </param>
        /// <param name="region2">
        /// The second plane to check.
        /// </param>
        /// <returns>
        /// True if regions lie on the same plane, else False.
        /// </returns>
        public static bool AreCoplanar(this Curve region1, Curve region2) {
            if (!region1.IsPlanar() || !region2.IsPlanar()) { return false; }
            Plane plane = region1.PlaneFromRegion();

            if (region2.PointAtStart.IsPtOnPlane(plane)) { return true; }
            return false;
        }
        /// <summary>
        /// Finds the segment IDs of a curve which are not linear (curved).
        /// </summary>
        /// <param name="crv">
        /// The curve to evaluate.
        /// </param>
        /// <returns>
        /// The segment IDs that are not linear.
        /// </returns>
        private static List<int> CurvedSegIDs(Curve crv) {

            Curve[] segments = crv.DuplicateSegments();
            List<int> curvedSegIDs = new List<int>();
            foreach (var kvp in segments.Select((value, index) => new { value, index })) {
                Curve seg = segments[kvp.index];
                Vector3d guide_vec = new Vector3d(seg.PointAtEnd) - new Vector3d(seg.PointAtStart);

                double[] parameters = seg.DivideByCount(3, false);
                foreach (double param in parameters) {
                    Vector3d tangent = seg.TangentAt(param);
                    if (!IsParallel(guide_vec, tangent)) {
                        curvedSegIDs.Add(kvp.index);
                        break;
                    }
                }
            }
            return curvedSegIDs;
        }
        /// <summary>
        /// Find the region that contains the voids. Prerequisite: All regions lie on the same plane.
        /// </summary>
        /// <param name="regions">
        /// The (closed) regions to be checked.
        /// </param>
        /// <returns>
        /// (a) The "master" region; (b) The voids of the "master" region; (c) The plane where the (closed) regions lie.
        /// </returns>
        internal static Tuple<Curve, Curve[], Plane> MasterRegionVoids(this Curve[] regions) {
            // Each polygon in voids MUST be included  by the masterArea
            if (regions.AreCoplanar()) {
                Curve masterArea;
                List<Curve> voids = new List<Curve>();
                Plane plane;
                if (regions.Length > 1) {
                    SortedList<double, Curve> SortedAreaPolygons = new SortedList<double, Curve>();
                    foreach (Curve polygon in regions) {
                        double area = AreaMassProperties.Compute(polygon).Area;
                        if (!SortedAreaPolygons.ContainsKey(area)) {
                            SortedAreaPolygons.Add(area, polygon);
                        }
                    }

                    // the polygon with the largest area is selected as the masterArea
                    masterArea = SortedAreaPolygons.Values[SortedAreaPolygons.Count - 1];
                    plane = masterArea.PlaneFromRegion();

                    if (masterArea.IsValid) {
                        // all other polygons contained in masterPolygon will be the voids
                        for (int i = 0; i < SortedAreaPolygons.Count - 1; i++) {
                            Curve polygon = SortedAreaPolygons.Values[i];
                            RegionContainment config = Curve.PlanarClosedCurveRelationship(masterArea, polygon, plane, Tolerance);
                            if (config == RegionContainment.BInsideA) {
                                voids.Add(polygon);
                            }
                        }
                    }
                    else {
                        // TODO
                        return null;
                    }
                }
                else {
                    masterArea = regions[0];
                    plane = masterArea.PlaneFromRegion();
                }
                return new Tuple<Curve, Curve[], Plane>(masterArea, voids.ToArray(), plane);
            }
            else {
                // TODO
                return null;
            }
        }
        /// <summary>
        /// Rethrn the number of curved segments that need to be tessellated,
        /// </summary>
        /// <param name="crv">
        /// The curve to evaluate
        /// </param>
        /// <returns>
        /// The number of curved segments.
        /// </returns>
        internal static int NoCurvedSegs(Curve crv) {
            return CurvedSegIDs(crv).Count;
        }
        /// <summary>
        /// Create a plane from a (closed) region
        /// </summary>
        /// <param name="region">
        /// The planar region to be evaluated.
        /// </param>
        /// <returns>
        /// The plane where the provided (closed) region lies on.
        /// </returns>
        internal static Plane PlaneFromRegion(this Curve region) {
            if (region.IsClosed && region.IsPlanar()) {
                Point3d pt1 = region.PointAtNormalizedLength(0);
                Point3d pt2 = region.PointAtNormalizedLength(0.33);
                Point3d pt3 = region.PointAtNormalizedLength(0.66);
                return new Plane(pt1, new Vector3d(pt2) - new Vector3d(pt1), new Vector3d(pt3) - new Vector3d(pt1));
            }
            else { return new Plane(); }
        }
    }
}