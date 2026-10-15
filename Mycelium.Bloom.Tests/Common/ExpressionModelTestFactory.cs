// ------------------------------------------------------------------------------------------------
// <copyright file="ExpressionModelTestFactory.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Common
{
    using System.Collections.Generic;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging.Abstractions;
    using Moq;
    using Mycelium.Bloom.Core.ModelLoading;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Dal;
    using SysML2.NET.Extensions;
    using SysML2.NET.Serializer.Json;

    internal static class ExpressionModelTestFactory
    {
        internal static INamespace LoadQuantities()
        {
            var environment = new Mock<IHostEnvironment>();
            environment.SetupGet(value => value.ContentRootPath).Returns(TestRepository.GetDirectoryPath("Mycelium.Bloom"));
            return new ModelLoaderService(environment.Object, new DeSerializer(NullLoggerFactory.Instance),
                new Assembler(NullLoggerFactory.Instance), NullLogger<ModelLoaderService>.Instance).LoadQuantitiesModel();
        }

        internal static TMembership Attach<TMembership>(IElement owner, params IElement[] children)
            where TMembership : IRelationship, new()
        {
            var membership = new TMembership();
            foreach (var child in children)
            {
                owner.AssignOwnership(membership, child);
            }
            return membership;
        }

        internal static IEnumerable<IElement> Descendants(IElement element)
        {
            yield return element;
            foreach (var child in element.ownedElement)
            {
                foreach (var nested in Descendants(child))
                {
                    yield return nested;
                }
            }
        }
    }
}
