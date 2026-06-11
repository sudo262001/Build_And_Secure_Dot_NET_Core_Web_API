using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using StudentAPI.DataSimulation;
using StudentAPI.Models;

namespace StudentAPI.Controllers
{
    //[Route("api/[controller]")]
    [Authorize]
    [Route("api/Students")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private static List<Student> _Students = StudentDataSimulation.StudentsList;

        [Authorize(Roles = "Admin")]
        [EnableCors("StudentApiCorsPolicy")]
        [HttpGet("All", Name = nameof(GetAllStudents))]
        [ProducesResponseType(typeof(IEnumerable<Student>), StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<Student>> GetAllStudents() => Ok(_Students);

        [AllowAnonymous]
        [EnableCors("StudentApiCorsPolicy2")]
        [HttpGet("Passed", Name = nameof(GetPassedStudents))]
        [ProducesResponseType(typeof(IEnumerable<Student>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public ActionResult<IEnumerable<Student>> GetPassedStudents()
        {
            var _PassedStudents = _Students.Where(s => s.Grade >= 50);
            if (!_PassedStudents.Any())
                return NotFound("No one passed");
            return Ok(_PassedStudents);
        }

        [AllowAnonymous]
        [HttpGet("Avg", Name = nameof(GetGradesAverage))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public ActionResult<double> GetGradesAverage() => Ok(_Students.Any() ? _Students.Average(s => s.Grade) : 0);
        

        [HttpGet("{id}", Name = nameof(GetStudentById))]
        [ProducesResponseType(typeof(Student), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public ActionResult<Student> GetStudentById(int id)
        {
            var Student = _Students.FirstOrDefault(s => s.Id == id);
            if (Student == null)
                return NotFound($"There is no student with Id: {id}");
            return Ok(Student);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost(Name = nameof(AddStudent))]
        [ProducesResponseType(typeof(Student), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public ActionResult<Student> AddStudent([FromBody] Student student)
        {
            if (student == null || student.Age < 0 || student.Grade < 0)
            {
                return BadRequest("Invalid Data");
            }
            student.Id = _Students.Any() ? _Students.Max(s => s.Id) + 1 : 1;
            _Students.Add(student);
            return CreatedAtRoute(nameof(GetStudentById), new { id = student.Id }, student);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}",Name = nameof(DeleteStudent))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public ActionResult<Student> DeleteStudent(int id)
        {
            var student = _Students.FirstOrDefault(s=> s.Id == id);
            if (student == null)
            {
                return NotFound($"Student with id {id} not found");
            }
            _Students.Remove(student);
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}", Name = nameof(UpdateStudent))]
        [ProducesResponseType(typeof(Student), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public ActionResult<Student> UpdateStudent(int id, [FromBody] Student updatedStudent)
        {
            if (id < 1 || updatedStudent == null || string.IsNullOrEmpty(updatedStudent.Name) || updatedStudent.Age < 0 || updatedStudent.Grade < 0)
            {
                return BadRequest("Invalid student data.");
            }

            var student = _Students.FirstOrDefault(s => s.Id == id);
            if (student == null)
            {
                return NotFound($"Student with ID {id} not found.");
            }

            student.Name = updatedStudent.Name;
            student.Age = updatedStudent.Age;
            student.Grade = updatedStudent.Grade;

            return Ok(student);
        }

    }
}
