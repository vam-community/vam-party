﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Party.Shared.Exceptions;
using Party.Shared.Results;
using Party.Shared.Utils;

namespace Party.Shared.Handlers
{
    public class PublishHandler
    {
        private readonly PartyConfiguration _config;
        private readonly IFileSystem _fs;

        public PublishHandler(PartyConfiguration config, IFileSystem fs)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
        }

        public async Task<PublishResult> PublishAsync(RegistryScript script, RegistryScriptVersion version, string path)
        {
            // TODO: Validate fields, especially the version and name
            if (script is null) throw new ArgumentNullException(nameof(script));
            if (version is null) throw new ArgumentNullException(nameof(version));
            if (path is null) throw new ArgumentNullException(nameof(path));

            if (!Path.IsPathRooted(path))
            {
                throw new InvalidOperationException($"Path must be rooted prior to being sent to this handler: {path}");
            }
            var savesDirectory = _config.VirtAMate.SavesDirectory;
            if (!path.StartsWith(savesDirectory))
            {
                throw new UserInputException($"Path must be inside the saves directory.\nPath: {path}\nSaves: {savesDirectory}");
            }

            var attrs = _fs.File.GetAttributes(path);
            string[] files;
            string name;
            var types = new[] { ".cs", ".cslist" };
            if (attrs.HasFlag(FileAttributes.Directory))
            {
                name = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar));
                files = _fs.Directory.GetFiles(path).Where(f => types.Contains(Path.GetExtension(f))).ToArray();
            }
            else if (attrs.HasFlag(FileAttributes.Normal))
            {
                if (types.Contains(Path.GetExtension(path)))
                {
                    name = Path.GetFileNameWithoutExtension(path);
                    files = new[] { path };
                }
                else
                {
                    name = null;
                    files = new string[0];
                }
            }
            else
            {
                throw new UserInputException("Specified file is neither a directory nor a file");
            }

            if (files.Length == 0)
            {
                throw new UserInputException("No files were found with either a .cs or a .cslist extension");
            }

            var registryFiles = new List<RegistryFile>();
            foreach (var file in files.OrderBy(s => s))
            {
                registryFiles.Add(new RegistryFile
                {
                    Filename = Path.GetFileName(file),
                    Url = "",
                    Hash = new RegistryFileHash
                    {
                        Type = "sha256",
                        Value = await Hashing.GetHashAsync(_fs, file).ConfigureAwait(false)
                    }
                });
            }

            if (script.Versions == null)
            {
                script.Versions = new[] { version }.ToList();
            }
            else
            {
                if (script.Versions.Any(v => v.Version == version.Version))
                {
                    throw new UserInputException("This version already exists in the registry.");
                }

                version.Files = registryFiles;
                script.Versions.Insert(0, version);
            }


            return new PublishResult
            {
                Formatted = JsonConvert.SerializeObject(script, Formatting.Indented)
            };
        }
    }
}
