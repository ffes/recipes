Recipes
=======

Simple recipe site generator from JSON+LD [recipe files](https://schema.org/Recipe).
Not very polished, just for personal use.

Written as a .NET 6 console app. Configure it with the `appsettings.json` file.

It goes through the input directory, parses all the JSON files in the tree and outputs the (Dutch) recipe in a readable html in the output directory and generates an `index.html`.
It also generates a `.epub` file that can be read on e-readers or using an app on your smartphone or tablet.
