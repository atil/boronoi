﻿using System;
using System.Collections.Generic;
using Assets.Helpers;
using UnityEngine;
using System.Linq;

namespace Assets.Scripts
{
    class Vector3Comparer : IEqualityComparer<Vector3>
    {
        private const float Tolerance = 10;
        public Vector3Comparer() { }
        public bool Equals(Vector3 x, Vector3 y)
        {
            return Math.Abs(x.x - y.x) < Tolerance && Math.Abs(x.z - y.z) < Tolerance;
        }

        public int GetHashCode(Vector3 obj)
        {
            return (int)(obj.x * 10000 + obj.z);
        }
    }

    class CenterComparer : IEqualityComparer<Center>
    {
        public CenterComparer() { }
        public bool Equals(Center x, Center y)
        {
            return GetHashCode(x) == GetHashCode(y);
        }

        public int GetHashCode(Center obj)
        {
            return (int)(obj.Point.x * 10000 + obj.Point.z);
        }
    }

    class CornerComparer : IEqualityComparer<Corner>
    {
        public bool Equals(Corner x, Corner y)
        {
            return Math.Abs(x.Point.x - y.Point.x) < 0.1 && Math.Abs(x.Point.z - y.Point.z) < 0.1;
        }

        public int GetHashCode(Corner obj)
        {
            return (int) (obj.Point.x * 10000 + obj.Point.z);
        }
    }

    class EdgeComparer : IEqualityComparer<Edge>
    {
        public EdgeComparer() { }
        public bool Equals(Edge x, Edge y)
        {
            return x == y;
        }

        public int GetHashCode(Edge obj)
        {
            return obj.GetHashCode();
        }
    }

    public interface IFactory
    {
        Center CenterFactory();
        Edge EdgeFactory(Corner begin, Corner end, Center Left, Center Right);
        Corner CornerFactory(float ax, float ay, float az);
    }

    public class DataFactory
    {
        private readonly Map _map;

        public DataFactory(Map map)
        {
            _map = map;
        }

        #region Implementation of IFactory

        public Center CenterFactory(Vector3 p)
        {
            if(_map.Centers.ContainsKey(p))
            {
                return _map.Centers[p];
            }
            else
            {
                var nc = new Center(p);
                _map.Centers.Add(nc.Point, nc);
                return nc;
            }
        }

        public Edge EdgeFactory(Corner begin, Corner end, Center left, Center right)
        {
            var midPoint = (begin.Point + end.Point)/2;
            if (_map.Edges.ContainsKey(midPoint))
            {
                return _map.Edges[midPoint];
            }
            else
            {
                var edge = new Edge(begin, end, left, right);
                _map.Edges.Add(edge.Midpoint, edge);
                return edge;
            }
        }

        public Corner CornerFactory(Vector3 p)
        {
            if (_map.Corners.ContainsKey(p))
            {
                return _map.Corners[p];
            }
            else
            {
                var nc = new Corner(p);
                _map.Corners.Add(nc.Point, nc);
                return nc;
            }
        }

        public Corner CloneCornerByCenter(Vector3 pos, Corner corner, Center center)
        {
            var nc = CornerFactory(pos + UnityEngine.Random.onUnitSphere * 5);
            nc.Touches.Add(center.Point, center);
            foreach (var c in corner.Adjacents.Values)
            {
                c.Adjacents.Remove(corner.Point);
                
                if (c.Touches.ContainsKey(center.Point))
                {
                    nc.Adjacents.Add(c.Point, c);
                    c.Adjacents.Add(nc.Point, nc);
                }
            }
            foreach (var b in corner.Protrudes.Values.Where(x => x.DelaunayStart == center || x.DelaunayEnd == center))
            {
                center.Borders.Remove(b.Midpoint);

                var bb = (b.VoronoiStart == corner)
                    ? EdgeFactory(nc, b.VoronoiEnd, b.DelaunayStart, b.DelaunayEnd)
                    : EdgeFactory(b.VoronoiStart, nc, b.DelaunayStart, b.DelaunayEnd);
                
                nc.Protrudes.Add(bb.Midpoint, bb);
                center.Borders.Add(bb.Midpoint, bb);
            }

            center.Corners.Remove(corner.Point);
            center.Corners.Add(nc.Point, nc);

            //_map.Corners.Remove(corner.Point);

            return nc;
        }

        public void RemoveEdge(Edge e)
        {
            _map.Edges.Remove(e.Midpoint);
        }

        public void RemoveCorner(Corner e)
        {
            _map.Corners.Remove(e.Point);
        }

        #endregion
    }
}
