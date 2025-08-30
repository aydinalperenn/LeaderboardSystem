using UnityEngine;
using System.Collections.Generic;


public class RowItemPool : MonoBehaviour
{
    private Stack<RowItemView> pool;
    private RowItemView prefab;
    private Transform parent;

    public RowItemPool(RowItemView prefab, Transform parent, int prewarm = 0)
    {
        this.prefab = prefab;
        this.parent = parent;
        this.pool = new Stack<RowItemView>();

        for (int i = 0; i < prewarm; i++)
        {
            var item = Object.Instantiate(prefab, parent);
            item.gameObject.SetActive(false);
            pool.Push(item);
        }
    }

    public RowItemView Get()
    {
        if (pool.Count > 0)
        {
            var it = pool.Pop();
            it.gameObject.SetActive(true);
            return it;
        }
        return Object.Instantiate(prefab, parent);
    }

    public void Release(RowItemView item)
    {
        item.gameObject.SetActive(false);
        pool.Push(item);
    }
}
