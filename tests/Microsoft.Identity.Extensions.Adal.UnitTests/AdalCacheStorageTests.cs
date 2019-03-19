﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Extensions.Adal.UnitTests
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Unit Testing")]
    [TestClass]
    public class AdalCacheStorageTests
    {
        public static readonly string CacheFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        private readonly TraceSource _logger = new TraceSource("TestSource");
        private static AdalStorageCreationProperties s_storageCreationProperties;

        [ClassInitialize]
        public static void ClassInitialize(TestContext _)
        {
            var builder = new AdalStorageCreationPropertiesBuilder("IdentityServiceAdalCache.cache");
            builder = builder.WithMacKeyChainAccountName("Microsoft.Developer.IdentityService");
            builder = builder.WithMacKeyChainAccountName("AdalCache");
            builder = builder.WithKeyringSchemaName("IdentityServiceAdalCache.cache");
            builder = builder.WithKeyringCollection("default");
            builder = builder.WithKeyringSecretLabel("AdalCache");
            builder = builder.WithKeyringAttributes("Microsoft.Developer.IdentityService", "1.0.0.0");
            s_storageCreationProperties = builder.Build();
        }

        [TestInitialize]
        public void TestInitialize()
        {
            CleanTestData();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            CleanTestData();
        }

        [TestMethod]
        public void AdalNewStoreNoFile()
        {
            var store = new AdalCacheStorage(s_storageCreationProperties, CacheFilePath, instanceName: null, logger: _logger);
            Assert.IsFalse(store.HasChanged);
            Assert.IsFalse(store.ReadData().Any());
        }

        [TestMethod]
        public void AdalWriteEmptyData()
        {
            var store = new AdalCacheStorage(s_storageCreationProperties, CacheFilePath, instanceName: null, logger: _logger);
            Assert.ThrowsException<ArgumentNullException>(() => store.WriteData(null));

            store.WriteData(new byte[0]);

            Assert.IsFalse(store.ReadData().Any());
        }

        [TestMethod]
        public void AdalWriteGoodData()
        {
            var store = new AdalCacheStorage(s_storageCreationProperties, CacheFilePath, instanceName: null, logger: _logger);
            Assert.ThrowsException<ArgumentNullException>(() => store.WriteData(null));

            byte[] data = { 2, 2, 3 };
            byte[] data2 = { 2, 2, 3, 4, 4 };
            store.WriteData(data);
            Assert.IsFalse(store.HasChanged);
            Enumerable.SequenceEqual(store.ReadData(), data);

            store.WriteData(data);
            Assert.IsFalse(store.HasChanged);
            store.WriteData(data2);
            Assert.IsFalse(store.HasChanged);
            store.WriteData(data);
            Assert.IsFalse(store.HasChanged);
            store.WriteData(data2);
            Assert.IsFalse(store.HasChanged);
            Enumerable.SequenceEqual(store.ReadData(), data2);
        }

        [TestMethod]
        public void AdalTestClear()
        {
            var store = new AdalCacheStorage(s_storageCreationProperties, CacheFilePath, instanceName: null, logger: _logger);
            var store2 = new AdalCacheStorage(s_storageCreationProperties, CacheFilePath, instanceName: null, logger: _logger);
            Assert.IsNotNull(Exception<ArgumentNullException>(() => store.WriteData(null)));

            byte[] data = { 2, 2, 3 };
            store.WriteData(data);

            Assert.IsFalse(store.HasChanged);
            Assert.IsTrue(store2.HasChanged);

            store2.ReadData();

            Enumerable.SequenceEqual(store.ReadData(), data);
            Assert.IsTrue(File.Exists(CacheFilePath));

            store.Clear();
            Assert.IsFalse(store.HasChanged);
            Assert.IsTrue(store2.HasChanged);

            Assert.IsFalse(store.ReadData().Any());
            Assert.IsFalse(store2.ReadData().Any());
            Assert.IsFalse(File.Exists(CacheFilePath));
        }

        [TestMethod]
        public void AdalTestRootSuffixWithNullSuffixCacheAndRegistry()
        {
            var store = new AdalCacheStorage(s_storageCreationProperties, CacheFilePath, instanceName: null, logger: _logger);
            Assert.AreEqual(store.CacheFilePath, CacheFilePath, ignoreCase: true, culture: CultureInfo.CurrentCulture);
            string localDataPath = SharedUtilities.GetDefaultArtifactPath();

            store = new AdalCacheStorage(s_storageCreationProperties, null, instanceName: string.Empty, logger: _logger);

            string cacheFilePath = Path.Combine(localDataPath, @"IdentityServiceAdalCache.cache");
            Assert.AreEqual(store.CacheFilePath, cacheFilePath, ignoreCase: true, culture: CultureInfo.CurrentCulture);

            store = new AdalCacheStorage(s_storageCreationProperties, null, instanceName: null, logger: _logger);
            Assert.AreEqual(store.CacheFilePath, cacheFilePath, ignoreCase: true, culture: CultureInfo.CurrentCulture);
        }

        [TestMethod]
        public void AdalTestRootSuffixWithGoodSuffixCacheAndRegistry()
        {
            var store = new AdalCacheStorage(s_storageCreationProperties, CacheFilePath, instanceName: "Exp", logger: _logger);
            Assert.AreEqual(store.CacheFilePath, CacheFilePath, ignoreCase: true, culture: CultureInfo.CurrentCulture);
            string localDataPath = SharedUtilities.GetDefaultArtifactPath();

            store = new AdalCacheStorage(s_storageCreationProperties, null, instanceName: "Exp", logger: _logger);
            string cacheFilePath = Path.Combine(localDataPath, "Exp", @"IdentityServiceAdalCache.cache");

            Assert.AreEqual(cacheFilePath, store.CacheFilePath, ignoreCase: true, culture: CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Records an exception thrown when executing the provided action
        /// </summary>
        /// <typeparam name="TException">The type of exception to record</typeparam>
        /// <param name="action">The action to execute</param>
        /// <returns>The exception if thrown; otherwise, null</returns>
        private static TException Exception<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
                return null;
            }
            catch (TException ex)
            {
                return ex;
            }
        }

        private void CleanTestData()
        {
            if (File.Exists(CacheFilePath))
            {
                File.Delete(CacheFilePath);
            }
        }
    }
}
