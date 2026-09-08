using Microsoft.AspNetCore.Mvc;

namespace MessServer.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class MoviesController : ControllerBase {
        private static List<Movie> _movies = new()
            {
                new Movie { Id = 1, Title = "Inception", Year = 2010 },
                new Movie { Id = 2, Title = "Interstellar", Year = 2014 }
            };

        // 1. GET: /api/movies (Получить все фильмы)
        [HttpGet]
        public ActionResult<IEnumerable<Movie>> GetAll () {
            return Ok(_movies);
        }

        // 2. GET: /api/movies/5 (Получить один фильм по ID)
        // {id} - это динамический сегмент
        [HttpGet("{id:int}")]
        public ActionResult<Movie> GetById (int id) {
            var movie = _movies.FirstOrDefault(m => m.Id == id);
            if (movie == null) return NotFound(); // 404
            return Ok(movie);
        }

        // 3. POST: /api/movies (Создать новый фильм)
        [HttpPost]
        public ActionResult Create ([FromBody] Movie newMovie) {
            newMovie.Id = _movies.Max(m => m.Id) + 1;
            _movies.Add(newMovie);
            // Возвращает статус 201 и ссылку на новый объект
            return CreatedAtAction(nameof(GetById), new { id = newMovie.Id }, newMovie);
        }

        // 4. PUT: /api/movies/5 (Обновить существующий фильм)
        [HttpPut("{id:int}")]
        public IActionResult Update (int id, [FromBody] Movie updatedMovie) {
            var movie = _movies.FirstOrDefault(m => m.Id == id);
            if (movie == null) return NotFound();

            movie.Title = updatedMovie.Title;
            movie.Year = updatedMovie.Year;

            return NoContent(); // 204 (успешно, без тела ответа)
        }

        // 5. DELETE: /api/movies/5 (Удалить фильм)
        [HttpDelete("{id:int}")]
        public IActionResult Delete (int id) {
            var movie = _movies.FirstOrDefault(m => m.Id == id);
            if (movie == null) return NotFound();

            _movies.Remove(movie);
            return NoContent();
        }

        // 6. Опциональный параметр: /api/movies/search?name=Inception
        [HttpGet("search")]
        public ActionResult Search ([FromQuery] string name) {
            var results = _movies.Where(m => m.Title.Contains(name, StringComparison.OrdinalIgnoreCase));
            return Ok(results);
        }
    }

    public class Movie {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Year { get; set; }
    }
}
