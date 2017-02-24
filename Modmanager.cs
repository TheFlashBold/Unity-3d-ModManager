using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class Modmanager : MonoBehaviour
{
    public static Dictionary<string, Dictionary<string, string>> ModConfig = new Dictionary<string, Dictionary<string, string>>();
    
    Dictionary<string, object> instancemod = new Dictionary<string, object>();
    Dictionary<string, System.Type> typemod = new Dictionary<string, System.Type>();

    bool m_isLoadingMod = false;
    WWW m_www;

    private void Awake()
    {
        DontDestroyOnLoad(this);
    }

    void Start ()
    {
        StartCoroutine(CheckModFolder());
    }
	
    IEnumerator CheckModFolder()
    {
        if (Directory.Exists("Mods"))
        {
            string[] mods = Directory.GetDirectories(Path.Combine(Environment.CurrentDirectory, "Mods"));

            foreach (string mod in mods)
            {
                while(m_isLoadingMod)
                {
                    yield return null;
                }

                if (File.Exists(Path.Combine(mod, "config.ini")))
                {
                    string[] configFile = File.ReadAllLines(Path.Combine(mod, "config.ini"));

                    Dictionary<string, string> config = new Dictionary<string, string>();

                    foreach (string line in configFile)
                    {
                        string l;

                        if (line.Contains("#") || line.Contains("//"))
                        {
                            l = line.Split(new string[] { "#" }, StringSplitOptions.None)[0];
                            l = l.Split(new string[] { "//" }, StringSplitOptions.None)[0];
                        }
                        else
                        {
                            l = line;
                        }

                        if (l.Contains("="))
                        {
                            string[] c = l.Split('=').Select(side => side.Trim()).ToArray();
                            config[c[0]] = c[1];
                        }
                    }

                    if(config.ContainsKey("LoadMod") && config["LoadMod"] == "false")
                    {
                        continue;
                    }

                    ModConfig.Add(config["ModName"], config);

                    if (config["ModAssembly"] != null)
                    {
                        StartCoroutine(LoadMod(@"file:///" + Path.Combine(mod, config["ModAssembly"]), config["ModName"]));
                    }
                    else
                    {
                        Debug.LogWarning("Modmanager: " + config["ModName"] + " has no ModAssembly, skipping ...");
                    }
                }
                else
                {
                    Debug.LogWarning("Modmanager: " + mod.Split('\\').Last() + " has no config.ini, skipping ...");
                }
            }
        }

        yield return null;
    }

    IEnumerator LoadMod(string path, string modName)
    {
        if (!m_isLoadingMod)
        {
            m_isLoadingMod = true;

            m_www = new WWW(path);

            while (!m_www.isDone)
            {
                yield return null;
            }

            Assembly assembly = LoadAssembly();
            if (assembly != null)
            {
                OnAssemblyLoaded(new WWWAssembly(m_www.url, assembly), modName);
            }
        }
        else
        {
            Debug.LogWarning("Modmanager: Already trying to load a mod!");
        }

        yield return null;
    }

    private Assembly LoadAssembly()
    {
        try
        {
            return Assembly.Load(m_www.bytes);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Modmanager: Error loading Mod " + m_www.url + "\n" + e.ToString());
            return null;
        }
    }

    void OnAssemblyLoaded(WWWAssembly loadedAssembly, string modName)
    {
        System.Type type = loadedAssembly.Assembly.GetType(modName + ".Main");
        
        object instance = loadedAssembly.Assembly.CreateInstance(modName + ".Main");
        
        FieldInfo fieldmodVersion = type.GetField("ModVersion");
        string modVersion = ModConfig[modName]["ModVersion"];

        Debug.Log("Modmanager: Mod successfully loaded: " + modName + " V" + modVersion);        

        typemod.Add(modName, type);
        instancemod.Add(modName, instance);
        
        MethodInfo start = type.GetMethod("Start");
        start.Invoke(instance, null);
        
        m_isLoadingMod = false;
    }
}

public class WWWAssembly
{
    private string m_URL;
    private Assembly m_Assembly;

    public string URL
    {
        get
        {
            return m_URL;
        }
    }

    public Assembly Assembly
    {
        get
        {
            return m_Assembly;
        }
    }

    public WWWAssembly(string url, Assembly assembly)
    {
        m_URL = url;
        m_Assembly = assembly;
    }
}