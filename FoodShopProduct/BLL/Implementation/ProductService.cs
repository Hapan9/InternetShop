﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using BLL.Dto;
using BLL.Interfaces;
using DAL.Interfaces;
using DAL.Models;
using Newtonsoft.Json;

namespace BLL.Implementation
{
    public class ProductService : IProductService
    {
        private readonly HttpClient _client;
        private readonly IMapper _mapper;
        private readonly IProductRepository _productRepository;
        private readonly IProductScoreService _productScoreService;

        public ProductService(IProductRepository repository, IMapper mapper, IProductScoreService scoreService)
        {
            _productRepository = repository;
            _mapper = mapper;
            _productScoreService = scoreService;
            _client = new HttpClient();
        }

        public async Task<Product> Get(Guid id)
        {
            if (await _productRepository.Get(id) == null)
                throw new ArgumentException();

            return await _productRepository.Get(id);
        }

        public async Task<IEnumerable<Product>> GetAll()
        {
            return await _productRepository.GetAll();
        }

        public async Task Create(ProductDto item)
        {
            await _productRepository.Create(_mapper.Map<Product>(item));
            await _productRepository.Save();
        }

        public async Task Update(Guid id, ProductDto item)
        {
            if (await _productRepository.Get(id) == null)
                throw new ArgumentException();

            await _productRepository.Update(_mapper.Map<Product>(item,
                opts => opts.AfterMap((_,
                    p) => p.Id = id)));

            await _productRepository.Save();
        }

        public async Task Delete(Guid id)
        {
            if (await _productRepository.Get(id) == null)
                throw new ArgumentException();

            await _productRepository.Delete(id);
            await _productRepository.Save();
        }

        public async Task<Product> GetWithScore(Guid id)
        {
            var productWithScore = await _productRepository.Get(id);
            if (productWithScore == null) throw new ArgumentNullException();

            var score = 0f;
            var productScores = (await _productScoreService.GetAll(id)).ToList();
            var userIds = productScores.Select(ps => ps.UserId).Distinct().ToList();
            var scores = await GetScores(userIds);
            if (scores == null) return productWithScore;

            productWithScore.ProductScores = null;


            for (var i = 0; i < userIds.Count(); i++)
                score += productScores.Where(ps => ps.UserId == userIds[i]).Select(ps => (int) ps.Score * 25).Sum() *
                    scores[i] / 100;

            score /= productScores.Count;

            productWithScore.Score = (int) score;

            return productWithScore;
        }

        public async Task<IEnumerable<Product>> GetAllWithScore()
        {
            var productsWithScore = (await _productRepository.GetAll()).ToList();

            foreach (var productWithScore in productsWithScore)
            {
                var score = 0f;
                var productScores = (await _productScoreService.GetAll(productWithScore.Id)).ToList();
                var userIds = productScores.Select(ps => ps.UserId).Distinct().ToList();
                var scores = await GetScores(userIds);
                if (scores == null) continue;
                productWithScore.ProductScores = null;
                if (scores.Count == 0)
                    continue;

                for (var i = 0; i < userIds.Count(); i++)
                    score += productScores.Where(ps => ps.UserId == userIds[i]).Select(ps => ((int) ps.Score + 1) * 20)
                        .Sum() * scores[i] / 100;

                score /= productScores.Count;

                productWithScore.Score = (int) score;
            }

            return productsWithScore;
        }

        private async Task<List<float>> GetScores(IEnumerable<Guid> ids)
        {
            try
            {
                var httpContent =
                    new StringContent(JsonConvert.SerializeObject(ids), Encoding.UTF8, "application/json");
                var response = await _client.PostAsync("https://localhost:44303/api/UserScore", httpContent);

                if (response.StatusCode == HttpStatusCode.OK)
                    return await response.Content.ReadFromJsonAsync<List<float>>();

                return null;
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}