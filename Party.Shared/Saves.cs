namespace Party.Shared
{
    public class Saves
    {
        public Scene[] Scenes { get; }
        public Script[] Scripts { get; }

        public Saves(Scene[] scene, Script[] script)
        {
            Scenes = scene;
            Scripts = script;
        }
    }
}
