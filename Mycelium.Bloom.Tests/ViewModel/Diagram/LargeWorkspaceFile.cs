// ------------------------------------------------------------------------------------------------
// <copyright file="LargeWorkspaceFile.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel.Diagram
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json;
    using System.Text.Json.Nodes;

    internal sealed class LargeWorkspaceFile : IDisposable
    {
        private readonly List<JsonObject> elements = [];
        private int sequence;

        public LargeWorkspaceFile(int count, int depth = 8)
        {
            var root = this.Element("Package", "Controlled large workspace");
            var parts = new JsonObject[count];
            var ports = new JsonObject[count];
            var packages = new List<JsonObject>();
            JsonObject package = null;
            for (var index = 0; index < count; index++)
            {
                if (index % 100 == 0)
                {
                    package = this.Element("Package", $"Region {index / 100:D3}");
                    packages.Add(package);
                    this.Own(root, package);
                }

                parts[index] = this.Element("PartUsage", $"part{index:D5}");
                parts[index]["isComposite"] = true;
                var parent = index > 0 && index < depth ? parts[index - 1] : package;
                this.Own(parent, parts[index], parent == package ? "OwningMembership" : "FeatureMembership");
                ports[index] = this.Element("PortUsage", $"port{index:D5}");
                ports[index]["direction"] = index % 2 == 0 ? "out" : "in";
                this.Own(parts[index], ports[index], "FeatureMembership");
                var attribute = this.Element("AttributeUsage", $"attribute{index:D5}");
                this.Own(parts[index], attribute, "FeatureMembership");
            }

            for (var index = 0; index + 1 < count; index += 2)
            {
                var connection = this.Element("ConnectionUsage", $"connection{index:D5}");
                this.Own(packages[index / 100], connection);
                foreach (var port in new[] { ports[index], ports[index + 1] })
                {
                    var end = this.Element("Feature", null);
                    end["isEnd"] = true;
                    this.Own(connection, end, "EndFeatureMembership");
                    var reference = this.Element("ReferenceSubsetting", null);
                    reference["owningRelatedElement"] = Ref(end);
                    reference["ownedRelatedElement"] = new JsonArray();
                    foreach (var key in new[] { "referencedFeature", "subsettedFeature", "general" })
                    {
                        reference[key] = Ref(port);
                    }

                    foreach (var key in new[] { "referencingFeature", "subsettingFeature", "specific" })
                    {
                        reference[key] = Ref(end);
                    }

                    ((JsonArray)end["ownedRelationship"]).Add(Ref(reference));
                }
            }

            this.Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".json");
            File.WriteAllText(this.Path, JsonSerializer.Serialize(this.elements));
        }

        public string Path { get; }
        public int ElementCount => this.elements.Count;
        public Uri Uri => new(this.Path);

        private JsonObject Element(string type, string name)
        {
            var id = $"70000000-0000-4000-8000-{++this.sequence:D12}";
            var element = new JsonObject
            {
                ["@type"] = type,
                ["@id"] = id,
                ["elementId"] = id,
                ["declaredName"] = name,
                ["ownedRelationship"] = new JsonArray()
            };
            this.elements.Add(element);
            return element;
        }

        private void Own(JsonObject owner, JsonObject child, string type = "OwningMembership")
        {
            var membership = this.Element(type, null);
            membership["owningRelatedElement"] = Ref(owner);
            membership["ownedRelatedElement"] = new JsonArray(Ref(child));
            membership["memberElement"] = Ref(child);
            membership["source"] = new JsonArray(Ref(owner));
            membership["target"] = new JsonArray(Ref(child));
            child["owningRelationship"] = Ref(membership);
            ((JsonArray)owner["ownedRelationship"]).Add(Ref(membership));
        }

        private static JsonObject Ref(JsonObject element) => new() { ["@id"] = element["@id"].GetValue<string>() };

        public void Dispose() => File.Delete(this.Path);
    }
}