﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit.Extensions
{
    public class ObservationTestCollectionRunner : TestCollectionRunner<ObservationTestCase>
    {
        readonly static RunSummary FailedSummary = new RunSummary { Total = 1, Failed = 1 };

        readonly IMessageSink diagnosticMessageSink;

        public ObservationTestCollectionRunner(ITestCollection testCollection,
                                               IEnumerable<ObservationTestCase> testCases,
                                               IMessageSink diagnosticMessageSink,
                                               IMessageBus messageBus,
                                               ITestCaseOrderer testCaseOrderer,
                                               ExceptionAggregator aggregator,
                                               CancellationTokenSource cancellationTokenSource)
            : base(testCollection, testCases, messageBus, testCaseOrderer, aggregator, cancellationTokenSource)
        {
            this.diagnosticMessageSink = diagnosticMessageSink;
        }

        protected override async Task<RunSummary> RunTestClassAsync(ITestClass testClass,
                                                                    IReflectionTypeInfo @class,
                                                                    IEnumerable<ObservationTestCase> testCases)
        {
            var timer = new ExecutionTimer();
            var specification = Activator.CreateInstance(testClass.Class.ToRuntimeType()) as Specification;
            if (specification == null)
            {
                Aggregator.Add(new InvalidOperationException(String.Format("Test class {0} cannot be static, and must derive from Specification.", testClass.Class.Name)));
                return FailedSummary;
            }

            var result = await new ObservationTestClassRunner(specification, testClass, @class, testCases, diagnosticMessageSink, MessageBus, TestCaseOrderer, new ExceptionAggregator(Aggregator), CancellationTokenSource).RunAsync();

            var disposable = specification as IDisposable;
            if (disposable != null)
                timer.Aggregate(disposable.Dispose);

            return result;
        }
    }
}
