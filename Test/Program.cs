using System;
using System.Reflection;

namespace Inspector
{
    class Program
    {
        static void Main(string[] args)
        {
            try {
                string dllPath = @"C:\Program Files (x86)\Steam\steamapps\common\7 Days to Die Dedicated Server\7DaysToDieServer_Data\Managed\Assembly-CSharp.dll";
                Assembly assembly = Assembly.LoadFrom(dllPath);

                Type tEnum = assembly.GetType("ModEvents+EModEventResult");
                if (tEnum != null)
                {
                    Console.WriteLine("Enum: " + tEnum.Name);
                    foreach (var name in Enum.GetNames(tEnum)) {
                        Console.WriteLine(name);
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
