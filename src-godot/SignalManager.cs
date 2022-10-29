using System;
using System.Reflection;
using Godot;

namespace Embot
{
    public class SignalManager
    {
        
    }

    public static class SignalExtensions
    {
        public static string GetName(this Signal signal)
        {
            return signal.GetType().GetMember(signal.ToString())[0].GetCustomAttribute<SignalName>().Name;
        }
    }

    public enum Signal
    {
        /// <summary>
        /// Valid on <see cref="Godot.RigidBody"/>
        /// </summary>
        [SignalName("body_entered")]
        BodyEntered
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class SignalName : Attribute
    {
        public string Name { get; private set; }
        
        public SignalName(string name)
        {
            this.Name = name;
        }
    }
}