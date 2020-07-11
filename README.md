# Crossword: Across Lite .puz to text file converter

This .Net Core console application converts one or more Across Lite .puz files into their text file equivalents.

The code is based on core functionality from XWord Info but is simplified for general use.

There are some rare Across Lite .puz files that cannot be fully represented by a text file alone. For example, some need to be tweaked after the fact by using Shift-F8 to add circles. The intention here is that correct text files will be created for every puzzle that <em>does</em> have a possible legit text representation, even ones that, say, have some squares with both rebus and circle properties.

For details on the text file format, see the PDF available from the Help menu in Across Lite.

Across Lite cannot represent many common crossword elements such as shaded squares, italicized clues, colors, images, etc.
