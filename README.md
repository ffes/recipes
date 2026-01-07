# Recipes

Simple recipe site generator from [schema.org](https://schema.org) based [Recipe JSON](https://schema.org/Recipe) files.
Not very polished, just for personal use.

Written as a .NET console app. Configure it with the `appsettings.json` file.

It goes through the input directory, parses all the JSON files in the tree and outputs the (Dutch) recipe in a readable html in the output directory and generates an `index.html`.
It can also generates a `.epub` file that can be read on e-readers or using an app on your smartphone or tablet.

To see the HTML generator in action, take a look at [our family cookbook](https://kookboek.fesevur.nl/), which is generated with the code in this repository and uses the content from https://gitlab.com/ffes/kookboek
