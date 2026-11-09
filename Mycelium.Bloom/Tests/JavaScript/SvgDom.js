// Copyright 2026 Starion Group S.A. SPDX-License-Identifier: Apache-2.0

export class SvgMatrix {
    constructor(a = 1, b = 0, c = 0, d = 1, e = 0, f = 0) {
        Object.assign(this, { a, b, c, d, e, f });
    }

    multiply(other) {
        return new SvgMatrix(this.a * other.a + this.c * other.b, this.b * other.a + this.d * other.b,
            this.a * other.c + this.c * other.d, this.b * other.c + this.d * other.d,
            this.a * other.e + this.c * other.f + this.e, this.b * other.e + this.d * other.f + this.f);
    }

    inverse() {
        const determinant = this.a * this.d - this.b * this.c;
        return new SvgMatrix(this.d / determinant, -this.b / determinant, -this.c / determinant, this.a / determinant,
            (this.c * this.f - this.d * this.e) / determinant, (this.b * this.e - this.a * this.f) / determinant);
    }

    translate(x, y) { return this.multiply(new SvgMatrix(1, 0, 0, 1, x, y)); }
    scale(value) { return this.scaleNonUniform(value, value); }
    scaleNonUniform(x, y) { return this.multiply(new SvgMatrix(x, 0, 0, y)); }
    rotate(degrees) {
        const angle = degrees * Math.PI / 180;
        return this.multiply(new SvgMatrix(Math.cos(angle), Math.sin(angle), -Math.sin(angle), Math.cos(angle)));
    }
}

export function installSvgDom(window) {
    const prototype = window.SVGElement.prototype;
    prototype.createSVGMatrix = () => new SvgMatrix();
    prototype.createSVGPoint = () => ({ x: 0, y: 0, matrixTransform(matrix) {
        return { x: matrix.a * this.x + matrix.c * this.y + matrix.e, y: matrix.b * this.x + matrix.d * this.y + matrix.f };
    } });
    prototype.createSVGTransform = () => ({ matrix: new SvgMatrix(), setMatrix(matrix) { this.matrix = matrix; } });
    prototype.getCTM = prototype.getScreenCTM = function () {
        const values = this.getAttribute('transform')?.match(/matrix\(([^)]+)\)/)?.[1].split(/[ ,]+/).map(Number);
        return values ? new SvgMatrix(...values) : new SvgMatrix();
    };
    prototype.getBBox = function () {
        return { x: Number(this.getAttribute('x') ?? 0), y: Number(this.getAttribute('y') ?? 0),
            width: Number(this.getAttribute('width') ?? this.textContent.length * 6), height: Number(this.getAttribute('height') ?? 12) };
    };
}
