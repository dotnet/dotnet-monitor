// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Linq;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.WebApi.UnitTests
{
    public class DefaultProcessConfigurationTests
    {
        [Fact]
        public void ConvertProcessKeyTest()
        {
            var processKey = new ProcessKey("processName");
            var filter = CreateFilterEntry(processKey);
            ValidateProcessFilter(DiagProcessFilterCriteria.ProcessName, processKey.ProcessName, filter);

            processKey = new ProcessKey(Guid.NewGuid());
            filter = CreateFilterEntry(processKey);
            ValidateProcessFilter(DiagProcessFilterCriteria.RuntimeId, processKey.RuntimeInstanceCookie.Value.ToString("D"), filter);

            processKey = new ProcessKey(5);
            filter = CreateFilterEntry(processKey);
            ValidateProcessFilter(DiagProcessFilterCriteria.ProcessId, processKey.ProcessId.Value.ToString(CultureInfo.InvariantCulture), filter);
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

            var filterDescriptorPidContains = new ProcessFilterDescriptor
            {
                Key = ProcessFilterKey.ProcessId,
                MatchType = ProcessFilterType.Contains,
                Value = "6"
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
            ValidateProcessFilter(DiagProcessFilterCriteria.ProcessId, filterDescriptorPid.Value, filter);

            //Contains still becomes Exact for ProcessId
            filter = CreateFilterEntry(filterDescriptorPidContains);
            ValidateProcessFilter(DiagProcessFilterCriteria.ProcessId, filterDescriptorPidContains.Value, filter);

            filter = CreateFilterEntry(filterDescriptorName);
            ValidateProcessFilter(DiagProcessFilterCriteria.ProcessName, filterDescriptorName.Value, filter);

            filter = CreateFilterEntry(filterDescriptorNameContains);
            ValidateProcessFilter(DiagProcessFilterCriteria.ProcessName, filterDescriptorNameContains.Value, DiagProcessFilterMatchType.Contains, filter);

            filter = CreateFilterEntry(filterDescriptorCommand);
            ValidateProcessFilter(DiagProcessFilterCriteria.CommandLine, filterDescriptorCommand.Value, filter);

            filter = CreateFilterEntry(filterDescriptorCommandContains);
            ValidateProcessFilter(DiagProcessFilterCriteria.CommandLine, filterDescriptorCommandContains.Value, DiagProcessFilterMatchType.Contains, filter);

            //This filter doesn't make any sense but we are just testing that we can combine multiple filters
            var options = CreateOptions(filterDescriptorPid, filterDescriptorName, filterDescriptorNameContains, filterDescriptorCommand, filterDescriptorCommandContains);

            ValidateProcessFilter(DiagProcessFilterCriteria.ProcessId, filterDescriptorPid.Value, options.Filters[0]);
            ValidateProcessFilter(DiagProcessFilterCriteria.ProcessName, filterDescriptorName.Value, options.Filters[1]);
            ValidateProcessFilter(DiagProcessFilterCriteria.ProcessName, filterDescriptorNameContains.Value, DiagProcessFilterMatchType.Contains, options.Filters[2]);
            ValidateProcessFilter(DiagProcessFilterCriteria.CommandLine, filterDescriptorCommand.Value, options.Filters[3]);
            ValidateProcessFilter(DiagProcessFilterCriteria.CommandLine, filterDescriptorCommandContains.Value, DiagProcessFilterMatchType.Contains, options.Filters[4]);
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
                DiagProcessFilterCriteria.CommandLine,
                DiagProcessFilterCriteria.ProcessName,
            };

            Assert.Equal(expectedValues.Length, actualValues.Length);
            for (int i = 0; i < expectedValues.Length; i++)
            {
                Assert.Equal(expectedValues.GetValue(i), actualValues[i]);
            }
        }

        private static void ValidateProcessFilter(DiagProcessFilterCriteria expectedCriteria,
            string expectedvalue,
            DiagProcessFilterEntry actualFilter)
        {
            ValidateProcessFilter(expectedCriteria, expectedvalue, DiagProcessFilterMatchType.Exact, actualFilter);
        }

        private static void ValidateProcessFilter(DiagProcessFilterCriteria expectedCriteria,
            string expectedvalue,
            DiagProcessFilterMatchType expectedMatchType,
            DiagProcessFilterEntry actualFilter)
        {
            Assert.Equal(expectedvalue, actualFilter.Value);
            Assert.Equal(expectedCriteria, actualFilter.Criteria);
            Assert.Equal(expectedMatchType, actualFilter.MatchType);
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
            foreach (var processFilter in filters)
            {
                filterOptions.Filters.Add(processFilter);
            }
            var filter = DiagProcessFilter.FromConfiguration(filterOptions);
            Assert.Equal(filters.Length, filter.Filters.Count);

            return filter;
        }
    }
}
