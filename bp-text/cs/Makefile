export TEXINPUTS=../tex//:

all: prace.pdf abstraktCZ.pdf abstraktEN.pdf

# LaTeX je potreba spustit nekolikrat, aby spravne spocital odkazy
prace.pdf: prace.tex $(wildcard *.tex) literatura.bib prace.xmpdata
	pdflatex $<
	bibtex prace
	pdflatex $<
	pdflatex $<

abstraktCZ.pdf: abstraktCZ.tex abstraktCZ.xmpdata
	pdflatex $<

abstraktEN.pdf: abstraktEN.tex abstraktEN.xmpdata
	pdflatex $<

clean:
	rm -f *.log *.dvi *.aux *.toc *.lof *.lot *.out *.bbl *.blg *.xmpi
	rm -f prace.pdf abstrakt.pdf
