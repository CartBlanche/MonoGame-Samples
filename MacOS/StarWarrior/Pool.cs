using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Artemis;
using System.Reflection;

namespace StarWarrior
{
    public class GamePool : ArtemisPool
    {
        Dictionary<Type, Bag<Component>> componentPool = new Dictionary<Type, Bag<Component>>();
        int limit = 0;
        Type[] components;

        public GamePool(int limit, Type[] components)
        {
            this.limit = limit;
            this.components = components;
        }

        public void Initialize()
        {
            foreach (Type type in components)
            {                
                MethodInfo methodInfo = GetType().GetMethod("AddComponentType");
                MethodInfo genericMethodInfo = methodInfo.MakeGenericMethod(new Type[] { type });
                genericMethodInfo.Invoke(this, null);
            }
            Populate(limit);
        }

        public void Populate(int quantity)
        {
            for (int i = 0; i < quantity; i++)
            {
                foreach (Type type in components)
                {
                    AddComponent(type, (Component)Activator.CreateInstance(type));
                }
            }
        }

        public void AddComponentType<T>() where T : Component
        {
            Bag<Component> bag = new Bag<Component>();
            componentPool.Add(typeof(T), bag);
        }

        public void AddComponent(Type type, Component c)
        {
            Bag<Component> bag;
            if (componentPool.TryGetValue(type, out bag))
            {
                bag.Add(c);
            }
        }

        public Component TakeComponent<T>() where T : Component
        {
            Bag<Component> bag;            
            if (componentPool.TryGetValue(typeof(T), out bag))
            {
                Component c = bag.RemoveLast();
                if (c == null)
                {
                    Populate((int)(limit * 0.25));
                    c = bag.RemoveLast();
                }
                return c;
            }
            else
            {
                return null;
            }
        }
    }
}