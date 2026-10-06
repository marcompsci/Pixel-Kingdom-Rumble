using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PKR
{
    /// <summary>
    /// Keyed GameObject pools for projectiles, particles, collectibles and common enemies.
    /// Pooled objects live under a persistent root and are cleared on every scene load so nothing
    /// survives into the next level.
    /// </summary>
    public static class PoolService
    {
        static readonly Dictionary<string, ObjectPool<GameObject>> Pools = new Dictionary<string, ObjectPool<GameObject>>();
        static Transform _root;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Pools.Clear();
            _root = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single) ClearAll();
        }

        static Transform Root
        {
            get
            {
                if (_root == null)
                {
                    var go = new GameObject("[PKR Pools]");
                    Object.DontDestroyOnLoad(go);
                    _root = go.transform;
                }
                return _root;
            }
        }

        /// <summary>Get an object from the pool for key, creating the pool (and objects) with create() as needed.</summary>
        public static GameObject Get(string key, Func<GameObject> create, int maxSize = 64)
        {
            if (!Pools.TryGetValue(key, out var pool))
            {
                pool = new ObjectPool<GameObject>(
                    createFunc: () =>
                    {
                        var go = create();
                        go.transform.SetParent(Root, false);
                        return go;
                    },
                    actionOnGet: go => go.SetActive(true),
                    actionOnRelease: go => go.SetActive(false),
                    actionOnDestroy: go => { if (go != null) Object.Destroy(go); },
                    collectionCheck: false,
                    defaultCapacity: 8,
                    maxSize: maxSize);
                Pools[key] = pool;
            }
            return pool.Get();
        }

        public static void Release(string key, GameObject go)
        {
            if (go == null) return;
            if (Pools.TryGetValue(key, out var pool)) pool.Release(go);
            else Object.Destroy(go);
        }

        public static int CountInactive(string key) => Pools.TryGetValue(key, out var p) ? p.CountInactive : 0;

        public static void ClearAll()
        {
            foreach (var p in Pools.Values) p.Clear();
            Pools.Clear();
            if (_root != null)
            {
                // Active (checked-out) objects aren't tracked by ObjectPool; destroy whatever is left.
                for (int i = _root.childCount - 1; i >= 0; i--) Object.Destroy(_root.GetChild(i).gameObject);
            }
        }
    }
}
