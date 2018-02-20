﻿using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Blissmo.Helper.KeyVault;
using Blissmo.SearchService.Interface;
using Blissmo.SearchService.Interface.Model;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Blissmo.SearchService.SearchProvider;

namespace Blissmo.SearchService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class SearchService : StatefulService, ISearchService
    {
        private ISearchProvider _searchProvider;
        private string _searchServiceName = KeyVault.GetValue("SearchServiceName");
        private string _adminApiKey = KeyVault.GetValue("SearchServiceKey");
        private string _indexName = "movie-index";

        public SearchService(StatefulServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[]
            {
                new ServiceReplicaListener(context => this.CreateServiceRemotingListener(context))
            };
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            _searchProvider = new SearchProvider.SearchProvider();

            var searchClient = await _searchProvider.CreateSearchServiceAsync(_searchServiceName, _adminApiKey);
            await _searchProvider.DeleteIndexIfExistsAsync(searchClient, _indexName);
            await _searchProvider.CreateIndexAsync<Movie>(searchClient, _indexName);

            ISearchIndexClient indexClient = searchClient.Indexes.GetClient(_indexName);
            await _searchProvider.SetDataSourceAsync(indexClient);
        }

        public async Task<IList<Movie>> SearchMovies(Interface.Model.SearchParameters searchParameters)
        {
            Microsoft.Azure.Search.Models.SearchParameters parameters;
            DocumentSearchResult<Movie> results;
            IList<Movie> resultSet = new List<Movie>();
            ISearchIndexClient indexClientForQueries = 
                await _searchProvider.CreateSearchIndexAsync(_searchServiceName, _adminApiKey, _indexName);

            parameters =
                new Microsoft.Azure.Search.Models.SearchParameters()
                {
                    Select = searchParameters.Select,
                    Filter = searchParameters.Filter,
                    OrderBy = searchParameters.OrderBy,
                    Top = searchParameters.Top,
                };

            results = await indexClientForQueries.Documents.SearchAsync<Movie>(searchParameters.SearchTerm, parameters);
            results.Results.ToList().ForEach(i =>
            {
                resultSet.Add(i.Document);
            });

            return resultSet;
        }
    }
}