// resolve_metadata.js
// Extracts program, course, and class from the file path for MBA vault structure.
// Place this in your Obsidian Templater scripts folder.

module.exports = async (tp) => {
    // Example path: "Program/Course/Class/filename.md"
    const pathParts = tp.file.path().split(/[\\/]/);
    // Adjust these indices if your vault root is deeper
    // e.g., ["MBA", "Program", "Course", "Class", "filename.md"]
    // Find the last three folder names before the file
    const len = pathParts.length;
    return {
        program: len > 3 ? pathParts[len - 4] : "",
        course: len > 2 ? pathParts[len - 3] : "",
        class: len > 1 ? pathParts[len - 2] : ""
    };
};
