# Crossword: Across Lite .puz to text file converter

This .Net Core console application converts one or more Across Lite .puz files into their text file equivalents.

The code includes a simplified version of core functionality from [XWord Info](https://www.xwordinfo.com "XWord Info") to parse the binary file and convert it to text. You now have a great head start if you want to research crossword data, create your own solving app, or even build your own XWord Info competitor!

For details on the text file format, see the PDF available from the Help menu in Across Lite.

There are some rare Across Lite .puz files that cannot be fully represented by text files alone. For example, some need to be tweaked after the fact by using Shift-8 (asterisk) to add circles. The intention here is that correct text files will be created for every puzzle that *does* have a possible legit text representation, even ones that, say, have some squares with both rebus and circle properties.

Across Lite cannot represent many common crossword elements such as shaded squares, italicized clues, colors, images, etc. It also cannot handle Acrostics or other non-standard forms.

Anyone interested in contributing to this project is welcome to submit a pull request. For more information, contact me via the email link on the [XWord Info](https://www.xwordinfo.com "XWord Info") home page.

If there is interest, more XWord Info core functionality will be moved to Open Source over time.

PLEASE RESPECT THE COPYRIGHTS ON PUBLISHED CROSSWORDS. You need permission from the rights holders for most public and for all commercial uses.

Jim Horne
