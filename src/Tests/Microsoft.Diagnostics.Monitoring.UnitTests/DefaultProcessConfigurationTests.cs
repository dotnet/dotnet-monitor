// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.RestServer;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.UnitTests
{
    public class DefaultProcessConfigurationTests
    {
        [Fact]
        public void ConvertProcessKeyTest()
        {
            var processKey = new ProcessKey("processName");
            var filter = CreateFilterEntry(processKey);
            ValidateProcessFilter(filter, DiagProcessFilterCriteria.ProcessName, processKey.ProcessName);

            processKey = new ProcessKey(Guid.NewGuid());
            filter = CreateFilterEntry(processKey);
            ValidateProcessFilter(filter, DiagProcessFilterCriteria.RuntimeId, processKey.RuntimeInstanceCookie.Value.ToString("D"));

            processKey = new ProcessKey(5);
            filter = CreateFilterEntry(processKey);
            ValidateProcessFilter(filter, DiagProcessFilterCriteria.ProcessId, processKey.ProcessId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        [Fact]
        public void ConvertProcessConfig()
        {
            var filterDescriptorPid = new ProcessFilterDescriptor
            {
                Key = ProcessFilterKey.ProcessId,
                MatchType = ProcessFilterType.Exact,
                Value = "5"
            };

            var filterDescriptorName = new ProcessFilterDescriptor
            {
                Key = ProcessFilterKey.ProcessName,
                MatchType = ProcessFilterType.Exact,
                Value = "name1"
            };

            var filterDescriptorNameContains = new ProcessFilterDescriptor
            {
                Key = ProcessFilterKey.ProcessName,
                MatchType = ProcessFilterType.Contains,
                Value = "namePartial"
            };

            var filterDescriptorCommand = new ProcessFilterDescriptor
            {
                Key = ProcessFilterKey.CommandLine,
                MatchType = ProcessFilterType.Exact,
                Value = "command arg"
            };

            var filterDescriptorCommandContains = new ProcessFilterDescriptor
            {
                Key = ProcessFilterKey.CommandLine,
                MatchType = ProcessFilterType.Contains,
                Value = "arg1"
            };

            var filter = CreateFilterEntry(filterDescriptorPid);
            ValidateProcessFilter(filter, DiagProcessFilterCriteria.ProcessId, filterDescriptorPid.Value);

            filter = CreateFilterEntry(filterDescriptorName);
            ValidateProcessFilter(filter, DiagProcessFilterCriteria.ProcessName, filterDescriptorName.Value);

            filter = CreateFilterEntry(filterDescriptorNameContains);
            ValidateProcessFilter(filter, DiagProcessFilterCriteria.ProcessName, filterDescriptorNameContains.Value, DiagProcessFilterMatchType.Contains);

            filter = CreateFilterEntry(filterDescriptorCommand);
            ValidateProcessFilter(filter, DiagProcessFilterCriteria.CommandLine, filterDescriptorCommand.Value);

            filter = CreateFilterEntry(filterDescriptorCommandContains);
            ValidateProcessFilter(filter, DiagProcessFilterCriteria.CommandLine, filterDescriptorCommandContains.Value, DiagProcessFilterMatchType.Contains);

            //This filter doesn't make any sense but we are just testing that we can combine multiple filters
            var options = CreateOptions(filterDescriptorPid, filterDescriptorName, filterDescriptorNameContains, filterDescriptorCommand, filterDescriptorCommandContains);

            ValidateProcessFilter(options.Filters[0], DiagProcessFilterCriteria.ProcessId, filterDescriptorPid.Value);
            ValidateProcessFilter(options.Filters[1], DiagProcessFilterCriteria.ProcessName, filterDescriptorName.Value);
            ValidateProcessFilter(options.Filters[2], DiagProcessFilterCriteria.ProcessName, filterDescriptorNameContains.Value, DiagProcessFilterMatchType.Contains);
            ValidateProcessFilter(options.Filters[3], DiagProcessFilterCriteria.CommandLine, filterDescriptorCommand.Value);
            ValidateProcessFilter(options.Filters[4], DiagProcessFilterCriteria.CommandLine, filterDescriptorCommandContains.Value, DiagProcessFilterMatchType.Contains);
        }

        [Fact]
        public void NewCriteriaTest()
        {
            //When new enumerations (such as Entrypoint) are added to DiagProcessFilterCriteria,
            //this test will fail. This means new unit tests should be written for that criteria and this test can be updated.

            //Sorted by unsigned integer value
            var expectedValues = Enum.GetValues(typeof(DiagProcessFilterCriteria));

            var actualValues = new[]
            {
                DiagProcessFilterCriteria.ProcessId,
                DiagProcessFilterCriteria.RuntimeId,
                DiagProcessFilterCriteria.ProcessName,
                DiagProcessFilterCriteria.CommandLine
            }.Cast<int>().ToArray();

            Assert.Equal(expectedValues.Length, actualValues.Length);
        }

        private static void ValidateProcessFilter(DiagProcessFilterEntry filter,
            DiagProcessFilterCriteria expectedCriteria,
            string expectedvalue,
            DiagProcessFilterMatchType expectedMatchType = DiagProcessFilterMatchType.Exact)
        {
            Assert.Equal(expectedvalue, filter.Value);
            Assert.Equal(expectedCriteria, filter.Criteria);
            Assert.Equal(expectedMatchType, filter.MatchType);
        }

        private static DiagProcessFilterEntry CreateFilterEntry(ProcessKey processKey)
        {
            var processFilter = DiagProcessFilter.FromProcessKey(processKey);
            Assert.Single(processFilter.Filters);
            return processFilter.Filters.First();
        }

        private static DiagProcessFilterEntry CreateFilterEntry(ProcessFilterDescriptor filter)
        {
            return CreateOptions(filter).Filters.First();
        }

        private static DiagProcessFilter CreateOptions(params ProcessFilterDescriptor[] filters)
        {
            var filterOptions = new ProcessFilterOptions();
            foreach(var processFilter in filters)
            {
                filterOptions.Filters.Add(processFilter);
            }
            var filter = DiagProcessFilter.FromConfiguration(filterOptions);
            Assert.Equal(filters.Length, filter.Filters.Count);

            return filter;
        }
    }
}
