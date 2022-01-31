using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApiAutores.DTOs;
using WebApiAutores.Entidades;
using WebApiAutores.Filtros;
using WebApiAutores.Utilidades;

namespace WebApiAutores.Controllers
{
    [ApiController]
    [Route("api/autores")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "EsAdmin")]
    //[Authorize]
    public class AutoresController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly IConfiguration configuration;

        public AutoresController(ApplicationDbContext context, IMapper mapper, IConfiguration configuration)
        {
            this.context = context;
            this.mapper = mapper;
            this.configuration = configuration;
        }

        //[HttpGet("configuraciones")]
        //public ActionResult<string> ObtenerConfiguracion()
        //{
        //    //return configuration["apellido"];
        //    return configuration["apellido"];
        //}

        [HttpGet(Name = "obtenerAutores")]
        [AllowAnonymous] // Para que no tome en cuenta el [Authorize]
        //[ResponseCache(Duration = 10)]
        //[ServiceFilter(typeof(MiFiltroDeAccion))]
        public async Task<ActionResult<List<AutorDTO>>> Get([FromQuery] PaginacionDTO paginacionDTO)
        {
            // Configuramos la paginacion de los autores
            var queryable = context.Autores.AsQueryable();
            await HttpContext.InsertarParametrosPaginacionEnCabecera(queryable);
            var autores = await queryable.OrderBy(autor => autor.Nombre).Paginar(paginacionDTO).ToListAsync();

            return mapper.Map<List<AutorDTO>>(autores);
        }

        

        // El "?" indica que es parámetro opcional
        [HttpGet("{id:int}", Name = "obtenerAutor")]
        public async Task<ActionResult<AutorDTOConLibros>> Get(int id)
        {
            var autor = await context.Autores
                .Include(autorDB => autorDB.AutoresLibros)
                .ThenInclude(autorLibroDB => autorLibroDB.Libro)
                .FirstOrDefaultAsync(x => x.Id == id);

            if(autor == null)
            {
                return NotFound();
            }

            // Generamos los enlaces
            //var dto = mapper.Map<AutorDTOConLibros>(autor);
            //GenerarEnlaces(dto);
            //return dto;

            return mapper.Map<AutorDTOConLibros>(autor);
        }

        // Este método es para generar los enlaces que se pueden generar de ese autor (HATEOAS)
        //private void GenerarEnlaces(AutorDTO autorDTO)
        //{
        //    autorDTO.Enlaces.Add(new DatosHATEOAS(
        //        enlace: Url.Link("obtenerAutor", new { id = autorDTO.Id }),
        //        descripcion: "self",
        //        metodo: "GET"));

        //    autorDTO.Enlaces.Add(new DatosHATEOAS(
        //        enlace: Url.Link("actualizarAutor", new { id = autorDTO.Id }),
        //        descripcion: "autor-actualizar",
        //        metodo: "PUT"));

        //    autorDTO.Enlaces.Add(new DatosHATEOAS(
        //        enlace: Url.Link("borrarAutor", new { id = autorDTO.Id }),
        //        descripcion: "self",
        //        metodo: "DELETE"));
        //}

        [HttpGet("{nombre}", Name = "obtenerAutorPorNombre")]
        public async Task<ActionResult<List<AutorDTO>>> Get([FromRoute] string nombre)
        {
            //var autor = await context.Autores.FirstOrDefaultAsync(x => x.Nombre.Contains(nombre));
            var autores = await context.Autores.Where(x => x.Nombre.Contains(nombre)).ToListAsync();

            //if (autor == null)
            //{
            //    return NotFound();
            //}

            return mapper.Map<List<AutorDTO>>(autores);
        }

        [HttpPost(Name = "crearAutor")]
        public async Task<ActionResult> Post([FromBody] AutorCreacionDTO autorCreacionDTO)
        {
            var existeAutorConElMismoNombre = await context.Autores.AnyAsync(x => x.Nombre == autorCreacionDTO.Nombre);

            if (existeAutorConElMismoNombre)
            {
                return BadRequest($"Ya existe un autor con el nombre {autorCreacionDTO.Nombre}");
            }

            //var autor = new Autor()
            //{
            //    Nombre = autorCreacionDTO.Nombre
            //};

            // Con el Mapper le decimos que lo que contiene el DTO van a ser los valores del Autor (Los identifica por los nombres Autor.Nombre, autorCreacionDTO.Nombre)
            var autor = mapper.Map<Autor>(autorCreacionDTO);

            context.Add(autor);
            await context.SaveChangesAsync();

            var autorDTO = mapper.Map<AutorDTO>(autor);

            //return Ok();
            return CreatedAtRoute("obtenerAutor", new { id = autor.Id }, autorDTO);
        }

        [HttpPut("{id:int}", Name = "actualizarAutor")] // api/autores/1
        public async Task<ActionResult> Put(AutorCreacionDTO autorCreacionDTO, int id)
        {
            //if(autor.Id != id)
            //{
            //    return BadRequest("El Id del autor no coincide con el id de la URL");
            //}

            // Verificamos si existe algún autor con el id que se ha recibido
            var existe = await context.Autores.AnyAsync(x => x.Id == id);

            if (!existe)
            {
                return NotFound();
            }

            var autor = mapper.Map<Autor>(autorCreacionDTO);
            autor.Id = id;

            context.Update(autor);
            await context.SaveChangesAsync();
            //return Ok();
            return NoContent();
        }

        [HttpDelete("{id:int}", Name = "borrarAutor")] // api/autores/2
        public async Task<ActionResult> Delete(int id)
        {
            // Verificamos si existe algún autor con el id que se ha recibido
            var existe = await context.Autores.AnyAsync(x => x.Id == id);

            if (!existe)
            {
                return NotFound();
            }

            // Si existe borramos el autor
            context.Remove(new Autor() { Id = id });
            await context.SaveChangesAsync();
            return NoContent();
        }
    }
}
