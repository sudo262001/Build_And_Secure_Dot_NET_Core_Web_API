using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using StudentAPI.DataSimulation;
using StudentAPI.Models;

namespace StudentAPI.Controllers
{
    //[Route("api/[controller]")]
    [Route("api/Students")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        [HttpGet("All", Name = "GetAllStudents")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<Student>> GetAllStudents()
        {
            return Ok(StudentDataSimulation.StudentsList);
        }

        [HttpGet("Passed", Name = "GetPassedStudents")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public ActionResult<IEnumerable<Student>> GetPassedStudents()
        {
            var _PassedStudents = StudentDataSimulation.StudentsList.Where(s => s.Grade >= 50);
            if (!_PassedStudents.Any())
                return NotFound("No one passed");
            return Ok(StudentDataSimulation.StudentsList);
        }
        [HttpGet("Avg", Name = "GetGradesAverage")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public ActionResult<double> GetGradesAverage()
        {
            var avg = StudentDataSimulation.StudentsList.Average(s => s.Grade);
            return Ok(avg);
        }
        [HttpGet("{id}", Name = "GetStudentById")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public ActionResult<Student> GetStudentById(int id)
        {
            int count = StudentDataSimulation.StudentsList.Count;
            if (id < 1 || id > count)
                return BadRequest($"Id: {id} is out of students range");
            var Student = StudentDataSimulation.StudentsList.FirstOrDefault(s => s.Id == id);
            if (Student == null)
                return NotFound($"There is no student with Id: {id}");
            return Ok(Student);
        }
        [HttpPost(Name = "AddStudent")]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public ActionResult<Student> AddStudent(Student student)
        {
            if (student == null || student.Age < 0 || student.Grade < 0)
            {
                return BadRequest("Invalid Data");
            }
            student.Id = StudentDataSimulation.StudentsList.Count > 0 ? StudentDataSimulation.StudentsList.Max(s => s.Id) + 1 : 1;
            StudentDataSimulation.StudentsList.Add(student);
            return CreatedAtRoute("GetStudentById", new { id = student.Id }, student);
        }
        [HttpDelete("{id}",Name = "DeleteStudent")]
        [ProducesResponseType(typeof(string), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public ActionResult<Student> DeleteStudent(int id)
        {
            if (id < 0 || id > StudentDataSimulation.StudentsList.Count)
            {
                return BadRequest("Invalid id");
            }
            var student = StudentDataSimulation.StudentsList[id];
            if (student == null)
            {
                return NotFound($"Student with id {id} not found");
            }
            StudentDataSimulation.StudentsList.Remove(student);
            return NoContent();
        }
        [HttpPut("{id}", Name = "UpdateStudent")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<Student> UpdateStudent(int id, Student updatedStudent)
        {
            if (id < 1 || updatedStudent == null || string.IsNullOrEmpty(updatedStudent.Name) || updatedStudent.Age < 0 || updatedStudent.Grade < 0)
            {
                return BadRequest("Invalid student data.");
            }

            var student = StudentDataSimulation.StudentsList.FirstOrDefault(s => s.Id == id);
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
