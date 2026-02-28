#!/usr/bin/env python3
from __future__ import annotations

import datetime as _dt
import re
from pathlib import Path
from typing import List, Tuple

from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_JUSTIFY
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import mm
from reportlab.platypus import (
    BaseDocTemplate,
    Frame,
    HRFlowable,
    ListFlowable,
    ListItem,
    PageBreak,
    PageTemplate,
    Paragraph,
    Preformatted,
    Spacer,
)

ROOT = Path(__file__).resolve().parents[2]
INPUT_MD = ROOT / "Packages/com.vareiko.foundation/Documentation~/USAGE_GUIDE.md"
OUTPUT_PDF = ROOT / "Packages/com.vareiko.foundation/Documentation~/USAGE_GUIDE.pdf"


class GuideDocTemplate(BaseDocTemplate):
    def __init__(self, filename: str, **kwargs):
        super().__init__(filename, **kwargs)
        frame = Frame(
            self.leftMargin,
            self.bottomMargin,
            self.width,
            self.height,
            id="normal",
        )
        self.addPageTemplates([PageTemplate(id="main", frames=[frame], onPage=self._draw_header_footer)])

    def _draw_header_footer(self, canvas, doc):
        canvas.saveState()

        # Top accent line
        canvas.setStrokeColor(colors.HexColor("#2E6DB4"))
        canvas.setLineWidth(2)
        canvas.line(doc.leftMargin, A4[1] - 18 * mm, A4[0] - doc.rightMargin, A4[1] - 18 * mm)

        # Header
        canvas.setFont("Helvetica-Bold", 9)
        canvas.setFillColor(colors.HexColor("#2E6DB4"))
        canvas.drawString(doc.leftMargin, A4[1] - 13.5 * mm, "Vareiko Foundation")

        canvas.setFont("Helvetica", 8)
        canvas.setFillColor(colors.HexColor("#555555"))
        canvas.drawRightString(A4[0] - doc.rightMargin, A4[1] - 13.5 * mm, "Usage Guide")

        # Footer
        canvas.setStrokeColor(colors.HexColor("#D6DEE8"))
        canvas.setLineWidth(0.7)
        canvas.line(doc.leftMargin, 14 * mm, A4[0] - doc.rightMargin, 14 * mm)

        canvas.setFont("Helvetica", 8)
        canvas.setFillColor(colors.HexColor("#666666"))
        canvas.drawString(doc.leftMargin, 9 * mm, f"Generated: {_dt.date.today().isoformat()}")
        canvas.drawRightString(A4[0] - doc.rightMargin, 9 * mm, f"Page {doc.page}")

        canvas.restoreState()


def _styles() -> dict:
    s = getSampleStyleSheet()
    return {
        "title": ParagraphStyle(
            "Title",
            parent=s["Title"],
            fontName="Helvetica-Bold",
            fontSize=30,
            leading=34,
            textColor=colors.HexColor("#0D2B45"),
            alignment=TA_CENTER,
            spaceAfter=10,
        ),
        "subtitle": ParagraphStyle(
            "Subtitle",
            parent=s["BodyText"],
            fontName="Helvetica",
            fontSize=12,
            leading=16,
            textColor=colors.HexColor("#334E68"),
            alignment=TA_CENTER,
            spaceAfter=18,
        ),
        "h1": ParagraphStyle(
            "H1",
            parent=s["Heading1"],
            fontName="Helvetica-Bold",
            fontSize=20,
            leading=24,
            textColor=colors.HexColor("#102A43"),
            spaceBefore=12,
            spaceAfter=8,
        ),
        "h2": ParagraphStyle(
            "H2",
            parent=s["Heading2"],
            fontName="Helvetica-Bold",
            fontSize=15,
            leading=19,
            textColor=colors.HexColor("#243B53"),
            spaceBefore=12,
            spaceAfter=6,
        ),
        "h3": ParagraphStyle(
            "H3",
            parent=s["Heading3"],
            fontName="Helvetica-Bold",
            fontSize=12,
            leading=15,
            textColor=colors.HexColor("#334E68"),
            spaceBefore=10,
            spaceAfter=5,
        ),
        "body": ParagraphStyle(
            "Body",
            parent=s["BodyText"],
            fontName="Helvetica",
            fontSize=10.6,
            leading=15,
            textColor=colors.HexColor("#1F2933"),
            alignment=TA_JUSTIFY,
            spaceAfter=5,
        ),
        "bullet": ParagraphStyle(
            "Bullet",
            parent=s["BodyText"],
            fontName="Helvetica",
            fontSize=10.6,
            leading=15,
            leftIndent=3,
            textColor=colors.HexColor("#1F2933"),
        ),
        "code": ParagraphStyle(
            "Code",
            parent=s["Code"],
            fontName="Courier",
            fontSize=8.7,
            leading=11,
            backColor=colors.HexColor("#F4F7FB"),
            borderColor=colors.HexColor("#D9E2EC"),
            borderWidth=0.7,
            borderPadding=8,
            leftIndent=2,
            rightIndent=2,
            spaceAfter=8,
        ),
        "meta": ParagraphStyle(
            "Meta",
            parent=s["BodyText"],
            fontName="Helvetica",
            fontSize=9,
            leading=13,
            textColor=colors.HexColor("#486581"),
            alignment=TA_CENTER,
            spaceAfter=2,
        ),
    }


def _inline_md_to_html(text: str) -> str:
    text = text.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")
    text = re.sub(r"`([^`]+)`", r"<font name='Courier'>\1</font>", text)
    text = re.sub(r"\*\*([^*]+)\*\*", r"<b>\1</b>", text)
    text = re.sub(r"\*([^*]+)\*", r"<i>\1</i>", text)
    return text


def _consume_list(lines: List[str], i: int) -> Tuple[int, List[Tuple[str, bool]]]:
    items: List[Tuple[str, bool]] = []
    while i < len(lines):
        line = lines[i]
        if re.match(r"^\s*-\s+", line):
            items.append((re.sub(r"^\s*-\s+", "", line).strip(), False))
            i += 1
            continue
        if re.match(r"^\s*\d+\.\s+", line):
            items.append((re.sub(r"^\s*\d+\.\s+", "", line).strip(), True))
            i += 1
            continue
        if not line.strip():
            i += 1
            break
        break
    return i, items


def build_pdf(input_md: Path, output_pdf: Path) -> None:
    styles = _styles()
    doc = GuideDocTemplate(
        str(output_pdf),
        pagesize=A4,
        leftMargin=18 * mm,
        rightMargin=18 * mm,
        topMargin=24 * mm,
        bottomMargin=18 * mm,
        title="Vareiko Foundation Usage Guide",
        author="Vareiko",
        subject="Unity package usage guide",
    )

    src = input_md.read_text(encoding="utf-8")
    lines = src.splitlines()

    story = []

    # Cover
    story.append(Spacer(1, 28 * mm))
    story.append(Paragraph("Vareiko Foundation", styles["title"]))
    story.append(Paragraph("Comprehensive Usage Guide", styles["subtitle"]))
    story.append(HRFlowable(width="45%", color=colors.HexColor("#2E6DB4"), thickness=1.2, spaceBefore=6, spaceAfter=10, hAlign="CENTER"))
    story.append(Paragraph("Version: 1.0.1", styles["meta"]))
    story.append(Paragraph(f"Date: {_dt.date.today().isoformat()}", styles["meta"]))
    story.append(Spacer(1, 70 * mm))
    story.append(Paragraph("Zenject-first architecture for fast, stable Unity project starts.", styles["subtitle"]))
    story.append(PageBreak())

    i = 0
    in_code = False
    code_lines: List[str] = []

    while i < len(lines):
        raw = lines[i]
        line = raw.rstrip("\n")

        if line.strip().startswith("```"):
            if not in_code:
                in_code = True
                code_lines = []
            else:
                in_code = False
                story.append(Preformatted("\n".join(code_lines), styles["code"]))
                code_lines = []
            i += 1
            continue

        if in_code:
            code_lines.append(line)
            i += 1
            continue

        if not line.strip():
            story.append(Spacer(1, 2))
            i += 1
            continue

        if line.startswith("# "):
            story.append(Paragraph(_inline_md_to_html(line[2:].strip()), styles["h1"]))
            i += 1
            continue

        if line.startswith("## "):
            story.append(Paragraph(_inline_md_to_html(line[3:].strip()), styles["h2"]))
            i += 1
            continue

        if line.startswith("### "):
            story.append(Paragraph(_inline_md_to_html(line[4:].strip()), styles["h3"]))
            i += 1
            continue

        if re.match(r"^\s*(-\s+|\d+\.\s+)", line):
            i, items = _consume_list(lines, i)
            list_items = []
            ordered = any(is_ordered for _, is_ordered in items)
            bullet_type = "1" if ordered else "bullet"
            for text, _ in items:
                list_items.append(ListItem(Paragraph(_inline_md_to_html(text), styles["bullet"])))
            story.append(
                ListFlowable(
                    list_items,
                    bulletType=bullet_type,
                    start="1",
                    leftIndent=14,
                    bulletFontName="Helvetica",
                    bulletFontSize=9,
                    bulletColor=colors.HexColor("#334E68"),
                    spaceAfter=6,
                )
            )
            continue

        story.append(Paragraph(_inline_md_to_html(line), styles["body"]))
        i += 1

    doc.build(story)


def main() -> None:
    if not INPUT_MD.exists():
        raise FileNotFoundError(f"Input guide not found: {INPUT_MD}")

    build_pdf(INPUT_MD, OUTPUT_PDF)
    print(str(OUTPUT_PDF))


if __name__ == "__main__":
    main()
