﻿/*
* Farseer Physics Engine based on Box2D.XNA port:
* Copyright (c) 2010 Ian Qvist
* 
* Box2D.XNA port of Box2D:
* Copyright (c) 2009 Brandon Furtwangler, Nathan Furtwangler
*
* Original source Box2D:
* Copyright (c) 2006-2009 Erin Catto http://www.gphysics.com 
* 
* This software is provided 'as-is', without any express or implied 
* warranty.  In no event will the authors be held liable for any damages 
* arising from the use of this software. 
* Permission is granted to anyone to use this software for any purpose, 
* including commercial applications, and to alter it and redistribute it 
* freely, subject to the following restrictions: 
* 1. The origin of this software must not be misrepresented; you must not 
* claim that you wrote the original software. If you use this software 
* in a product, an acknowledgment in the product documentation would be 
* appreciated but is not required. 
* 2. Altered source versions must be plainly marked as such, and must not be 
* misrepresented as being the original software. 
* 3. This notice may not be removed or altered from any source distribution. 
*/

using System;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;

namespace FarseerPhysics.Collision
{
    internal struct Pair : IComparable<Pair>
    {
        public int ProxyIdA;
        public int ProxyIdB;

        #region IComparable<Pair> Members

        public int CompareTo(Pair other)
        {
            if (ProxyIdA < other.ProxyIdA)
            {
                return -1;
            }
            if (ProxyIdA == other.ProxyIdA)
            {
                if (ProxyIdB < other.ProxyIdB)
                {
                    return -1;
                }
                if (ProxyIdB == other.ProxyIdB)
                {
                    return 0;
                }
            }

            return 1;
        }

        #endregion
    }

    /// <summary>
    /// The broad-phase is used for computing pairs and performing volume queries and ray casts.
    /// This broad-phase does not persist pairs. Instead, this reports potentially new pairs.
    /// It is up to the client to consume the new pairs and to track subsequent overlap.
    /// </summary>
    public class DynamicTreeBroadPhase : IBroadPhase
    {
        private int[] _moveBuffer;
        private int _moveCapacity;
        private int _moveCount;

        private Pair[] _pairBuffer;
        private int _pairCapacity;
        private int _pairCount;
        private int _proxyCount;
        private Func<int, bool> _queryCallback;
        private int _queryProxyId;
        private DynamicTree<FixtureProxy> _tree = new DynamicTree<FixtureProxy>();

        public DynamicTreeBroadPhase()
        {
            _queryCallback = new Func<int, bool>(QueryCallback);

            _pairCapacity = 16;
            _pairBuffer = new Pair[_pairCapacity];

            _moveCapacity = 16;
            _moveBuffer = new int[_moveCapacity];
        }

        #region IBroadPhase Members

        /// <summary>
        /// Get the number of proxies.
        /// </summary>
        /// <value>The proxy count.</value>
        public int ProxyCount
        {
            get { return _proxyCount; }
        }

        /// <summary>
        /// Create a proxy with an initial AABB. Pairs are not reported until
        /// UpdatePairs is called.
        /// </summary>
        /// <param name="aabb">The aabb.</param>
        /// <param name="proxy">The user data.</param>
        /// <returns></returns>
        public int AddProxy(ref FixtureProxy proxy)
        {
            int proxyId = _tree.AddProxy(ref proxy.AABB, proxy);
            ++_proxyCount;
            BufferMove(proxyId);
            return proxyId;
        }

        /// <summary>
        /// Destroy a proxy. It is up to the client to remove any pairs.
        /// </summary>
        /// <param name="proxyId">The proxy id.</param>
        public void RemoveProxy(int proxyId)
        {
            UnBufferMove(proxyId);
            --_proxyCount;
            _tree.RemoveProxy(proxyId);
        }

        public void MoveProxy(int proxyId, ref AABB aabb, Vector2 displacement)
        {
            bool buffer = _tree.MoveProxy(proxyId, ref aabb, displacement);
            if (buffer)
            {
                BufferMove(proxyId);
            }
        }

        /// <summary>
        /// Get the AABB for a proxy.
        /// </summary>
        /// <param name="proxyId">The proxy id.</param>
        /// <param name="aabb">The aabb.</param>
        public void GetFatAABB(int proxyId, out AABB aabb)
        {
            _tree.GetFatAABB(proxyId, out aabb);
        }

        /// <summary>
        /// Get user data from a proxy. Returns null if the id is invalid.
        /// </summary>
        /// <param name="proxyId">The proxy id.</param>
        /// <returns></returns>
        public FixtureProxy GetProxy(int proxyId)
        {
            return _tree.GetUserData(proxyId);
        }

        /// <summary>
        /// Test overlap of fat AABBs.
        /// </summary>
        /// <param name="proxyIdA">The proxy id A.</param>
        /// <param name="proxyIdB">The proxy id B.</param>
        /// <returns></returns>
        public bool TestOverlap(int proxyIdA, int proxyIdB)
        {
            AABB aabbA, aabbB;
            _tree.GetFatAABB(proxyIdA, out aabbA);
            _tree.GetFatAABB(proxyIdB, out aabbB);
            return AABB.TestOverlap(ref aabbA, ref aabbB);
        }

        /// <summary>
        /// Update the pairs. This results in pair callbacks. This can only add pairs.
        /// </summary>
        /// <param name="callback">The callback.</param>
        public void UpdatePairs(BroadphaseDelegate callback)
        {
            // Reset pair buffer
            _pairCount = 0;

            // Perform tree queries for all moving proxies.
            for (int j = 0; j < _moveCount; ++j)
            {
                _queryProxyId = _moveBuffer[j];
                if (_queryProxyId == -1)
                {
                    continue;
                }

                // We have to query the tree with the fat AABB so that
                // we don't fail to create a pair that may touch later.
                AABB fatAABB;
                _tree.GetFatAABB(_queryProxyId, out fatAABB);

                // Query tree, create pairs and add them pair buffer.
                _tree.Query(_queryCallback, ref fatAABB);
            }

            // Reset move buffer
            _moveCount = 0;

            // Sort the pair buffer to expose duplicates.
            Array.Sort(_pairBuffer, 0, _pairCount);

            // Send the pairs back to the client.
            int i = 0;
            while (i < _pairCount)
            {
                Pair primaryPair = _pairBuffer[i];
                FixtureProxy userDataA = _tree.GetUserData(primaryPair.ProxyIdA);
                FixtureProxy userDataB = _tree.GetUserData(primaryPair.ProxyIdB);

                callback(ref userDataA, ref userDataB);
                ++i;

                // Skip any duplicate pairs.
                while (i < _pairCount)
                {
                    Pair pair = _pairBuffer[i];
                    if (pair.ProxyIdA != primaryPair.ProxyIdA || pair.ProxyIdB != primaryPair.ProxyIdB)
                    {
                        break;
                    }
                    ++i;
                }
            }

            // Try to keep the tree balanced.
            _tree.Rebalance(4);
        }

        /// <summary>
        /// Query an AABB for overlapping proxies. The callback class
        /// is called for each proxy that overlaps the supplied AABB.
        /// </summary>
        /// <param name="callback">The callback.</param>
        /// <param name="aabb">The aabb.</param>
        public void Query(Func<int, bool> callback, ref AABB aabb)
        {
            _tree.Query(callback, ref aabb);
        }

        /// <summary>
        /// Ray-cast against the proxies in the tree. This relies on the callback
        /// to perform a exact ray-cast in the case were the proxy contains a shape.
        /// The callback also performs the any collision filtering. This has performance
        /// roughly equal to k * log(n), where k is the number of collisions and n is the
        /// number of proxies in the tree.
        /// </summary>
        /// <param name="callback">A callback class that is called for each proxy that is hit by the ray.</param>
        /// <param name="input">The ray-cast input data. The ray extends from p1 to p1 + maxFraction * (p2 - p1).</param>
        public void RayCast(Func<RayCastInput, int, float> callback, ref RayCastInput input)
        {
            _tree.RayCast(callback, ref input);
        }

        public void TouchProxy(int proxyId)
        {
            BufferMove(proxyId);
        }

        #endregion

        /// <summary>
        /// Compute the height of the embedded tree.
        /// </summary>
        /// <returns></returns>
        public int ComputeHeight()
        {
            return _tree.ComputeHeight();
        }

        private void BufferMove(int proxyId)
        {
            if (_moveCount == _moveCapacity)
            {
                int[] oldBuffer = _moveBuffer;
                _moveCapacity *= 2;
                _moveBuffer = new int[_moveCapacity];
                Array.Copy(oldBuffer, _moveBuffer, _moveCount);
            }

            _moveBuffer[_moveCount] = proxyId;
            ++_moveCount;
        }

        private void UnBufferMove(int proxyId)
        {
            for (int i = 0; i < _moveCount; ++i)
            {
                if (_moveBuffer[i] == proxyId)
                {
                    _moveBuffer[i] = -1;
                    return;
                }
            }
        }

        private bool QueryCallback(int proxyId)
        {
            // A proxy cannot form a pair with itself.
            if (proxyId == _queryProxyId)
            {
                return true;
            }

            // Grow the pair buffer as needed.
            if (_pairCount == _pairCapacity)
            {
                Pair[] oldBuffer = _pairBuffer;
                _pairCapacity *= 2;
                _pairBuffer = new Pair[_pairCapacity];
                Array.Copy(oldBuffer, _pairBuffer, _pairCount);
            }

            _pairBuffer[_pairCount].ProxyIdA = Math.Min(proxyId, _queryProxyId);
            _pairBuffer[_pairCount].ProxyIdB = Math.Max(proxyId, _queryProxyId);
            ++_pairCount;

            return true;
        }
    }
}