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
    using System.Linq;
    using SysML2.NET.Common;
    using SysML2.NET.Core.Core.Types;
    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Core.Types;
    using SysML2.NET.Core.POCO.Kernel.Packages;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.Attributes;
    using SysML2.NET.Core.POCO.Systems.Connections;
    using SysML2.NET.Core.POCO.Systems.Parts;
    using SysML2.NET.Core.POCO.Systems.Ports;
    using SysML2.NET.Dal;
    using SysML2.NET.Extensions;
    using SysML2.NET.Serializer.Json;

    internal sealed class LargeWorkspaceFile : IDisposable
    {
        private readonly List<IElement> elements = [];
        private int sequence;

        public LargeWorkspaceFile(int count, int depth = 8)
        {
            var root = this.Element<Package>("Controlled large workspace");
            var parts = new PartUsage[count];
            var ports = new PortUsage[count];
            var packages = new List<Package>();
            Package package = null;
            for (var index = 0; index < count; index++)
            {
                if (index % 100 == 0)
                {
                    package = this.Element<Package>($"Region {index / 100:D3}");
                    packages.Add(package);
                    root.AssignOwnership(this.Element<OwningMembership>(), package);
                }

                var part = parts[index] = this.Element<PartUsage>($"part{index:D5}");
                part.IsComposite = true;
                if (index > 0 && index < depth)
                {
                    parts[index - 1].AssignOwnership(this.Element<FeatureMembership>(), part);
                }
                else
                {
                    package.AssignOwnership(this.Element<OwningMembership>(), part);
                }

                var port = ports[index] = this.Element<PortUsage>($"port{index:D5}");
                port.Direction = index % 2 == 0 ? FeatureDirectionKind.Out : FeatureDirectionKind.In;
                part.AssignOwnership(this.Element<FeatureMembership>(), port);
                part.AssignOwnership(this.Element<FeatureMembership>(),
                    this.Element<AttributeUsage>($"attribute{index:D5}"));
            }

            for (var index = 0; index + 1 < count; index += 2)
            {
                var connection = this.Element<ConnectionUsage>($"connection{index:D5}");
                packages[index / 100].AssignOwnership(this.Element<OwningMembership>(), connection);
                this.AddEnd(connection, ports[index]);
                this.AddEnd(connection, ports[index + 1]);
            }

            this.Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".json");
            using var stream = File.Create(this.Path);
            new Serializer().Serialize(this.elements.Select(ToDto), SerializationModeKind.JSON, false, stream, default);
        }

        public string Path { get; }
        public int ElementCount => this.elements.Count;
        public Uri Uri => new(this.Path);

        private T Element<T>(string name = null) where T : IElement, new()
        {
            var id = Guid.Parse($"70000000-0000-4000-8000-{++this.sequence:D12}");
            var element = new T { Id = id, ElementId = id.ToString(), DeclaredName = name };
            this.elements.Add(element);
            return element;
        }

        private void AddEnd(ConnectionUsage connection, PortUsage port)
        {
            var end = this.Element<Feature>();
            end.IsEnd = true;
            connection.AssignOwnership(this.Element<EndFeatureMembership>(), end);
            var reference = this.Element<ReferenceSubsetting>();
            reference.ReferencedFeature = port;
            end.AssignOwnership(reference);
        }

        private static IData ToDto(IElement element) => element switch
        {
            Package value => value.ToDto(false),
            PartUsage value => value.ToDto(false),
            PortUsage value => value.ToDto(false),
            AttributeUsage value => value.ToDto(false),
            ConnectionUsage value => value.ToDto(false),
            Feature value => value.ToDto(false),
            ReferenceSubsetting value => value.ToDto(false),
            EndFeatureMembership value => value.ToDto(false),
            FeatureMembership value => value.ToDto(false),
            OwningMembership value => value.ToDto(false),
            _ => throw new InvalidOperationException("Unsupported test element.")
        };

        public void Dispose() => File.Delete(this.Path);
    }
}
