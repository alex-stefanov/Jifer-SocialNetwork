﻿namespace Jifer.Data.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using Microsoft.EntityFrameworkCore;
    using Jifer.Data.Constants;

    /// <summary>
    /// Representation of  a post made by a user.
    /// </summary>
    [Comment("JGo Class")]
    public class JGo
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public JGo() { }

        /// <summary>
        /// Initializes an instance of the JGo class.
        /// </summary>
        /// <param name="author">The user who created the JGo.</param>
        /// <param name="text">The content of the JGo.</param>
        public JGo(JUser author,string text)
        {
            this.Author = author;
            this.PublishDate = DateTime.Now;
            this.Text = text;
        }

        /// <summary>
        /// JGo Id => Primary Key
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Date/time of publishing the JGo.
        /// </summary>
        [Required]
        [DataType(DataType.Date)]
        [Comment("Date and time of publishing")]
        public DateTime PublishDate { get; set; }

        /// <summary>
        /// Id of the author.
        /// </summary>
        [Required]
        [Comment("Id of the author")]
        public string AuthorId { get; set; }

        /// <summary>
        /// Creator of the JGo.
        /// </summary>
        [Required]
        [ForeignKey("AuthorId")]
        [Comment("User who authored the post")]
        public JUser Author { get; set; } = null!;

        /// <summary>
        /// Information that is held in the JGo.
        /// </summary>
        [Required]
        [MaxLength(1500)]
        [Comment("Content of the JGo")]
        public string Text { get; set; } = null!;

        /// <summary>
        /// Visibility level.
        /// </summary>
        [Required]
        [Comment("Visibility of the post")]
        public ValidationConstants.Accessibility Visibility { get; set; }

    }
}