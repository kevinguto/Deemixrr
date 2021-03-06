﻿using System;
using System.Linq;
using System.Threading.Tasks;

using AutoMapper;

using Deemixrr.Data;
using Deemixrr.Jobs.BackgroundJobs;
using Deemixrr.Jobs.Models;
using Deemixrr.Models;
using Deemixrr.Repositories;
using Deemixrr.Services;

using Hangfire;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;

namespace Deemixrr.Controllers
{

    [Authorize]
    public class ArtistController : Controller
    {
        private readonly ILogger<ArtistController> _logger;
        private readonly IDataRepository _dataRepository;
        private readonly IDeezerApiService _deezerApiService;
        private readonly IMapper _mapper;

        public ArtistController(ILogger<ArtistController> logger, IDataRepository dataRepository, IDeezerApiService deezerApiService, IMapper mapper)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dataRepository = dataRepository ?? throw new ArgumentNullException(nameof(dataRepository));
            _deezerApiService = deezerApiService ?? throw new ArgumentNullException(nameof(deezerApiService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }


        [HttpGet]
        public async Task<IActionResult> Test()
        {
            var artists = await _dataRepository.GetArtists();

            foreach (var artist in artists)
            {
                BackgroundJob.Enqueue<CheckArtistForUpdatesBackgroundJob>(x => x.Execute(artist.DeezerId, false));
            }

            return View(nameof(Index), new ArtistIndexInputViewModel
            {
                Artists = artists
            });
        }


        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var artists = await _dataRepository.GetArtists(0, 100);

            return View(new ArtistIndexInputViewModel
            {
                Artists = artists
            });
        }

        [HttpPost]
        public async Task<IActionResult> Index(ArtistIndexInputViewModel model)
        {
            var artists = await _dataRepository.GetArtists(model.SearchTerm);

            return View(new ArtistIndexInputViewModel
            {
                Artists = artists
            });
        }


        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var folders = await _dataRepository.GetFolders();
            if (!folders.Any()) return View("AddFolderFirst");

            return View(new ArtistCreateInputModel
            {
                Folders = new SelectList(folders, nameof(Folder.Id), nameof(Folder.NamePath))
            });
        }

        [HttpPost]
        public IActionResult Create(ArtistCreateInputModel model)
        {
            if (!ModelState.IsValid) return BadRequest();

            BackgroundJob.Enqueue<CreateArtistBackgroundJob>(x => x.Execute(_mapper.Map<CreateArtistBackgroundJobData>(model), false));

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            var artist = await _dataRepository.GetArtist(id);
            if (artist == null) return View("Error");

            return View(new ArtistDeleteViewModel()
            {
                Artist = artist
            });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(ArtistDeleteViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var artist = await _dataRepository.GetArtist(model.Artist.Id);
            if (artist == null) return BadRequest();

            await _dataRepository.DeleteArtist(artist);

            return RedirectToAction(nameof(Index));
        }

    }
}