# DictionaryToObjectConverter
Convert a dictionary to Object in C#

Here's a helper class that converts any Dictionary to C# Object.

Take note: The Object class you're converting the Dictionary to should already be available in your project.

Sample usage:
DictionaryToObjectConverter converter = new DictionaryToObjectConverter();
YouCustomClass customCLass = converter.Convert<YouCustomClass>(theDictionaryContainingTheDataForTheClass);

Enjoy!
